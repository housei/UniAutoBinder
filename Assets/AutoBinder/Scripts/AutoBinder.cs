using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniAutoBinder
{
	/// <summary>
	/// I auto binder.
	/// </summary>
	public interface IAutoBinder
	{
		void Bind(object monoBehaviour);
	}
	
	/// <summary>
	/// Default auto binder.
	/// カスタマイズしたい場合は継承してvirtualメソッドをoverrideした方が簡単
	/// </summary>
	public class DefaultAutoBinder:IAutoBinder
	{
		private MonoBehaviour _monoBehaviour;

		// key=Prefab Filename(remote extention) value= Prefab FilePath
		private Dictionary<string,string> _prefabPathMap;
		
		//
		// Bind実行
		//
		public virtual void Bind(object monoBehaviour)
		{
#if UNITY_EDITOR			
			System.Diagnostics.Stopwatch stw = new System.Diagnostics.Stopwatch();
			stw.Start();
#endif
			_monoBehaviour = monoBehaviour as MonoBehaviour;
			if ( _monoBehaviour==null ){ return; }
						
			FieldInfo[] fields = GetFields( _monoBehaviour );
			if ( fields==null || fields.Length==0 ){ return; }
			
			// 対象外のFieldを判定するためのMethodを取得する
			MethodInfo ignoreRuleMethod = GetIgnoreRuleMethod( _monoBehaviour );

			// BindするField情報を作成
			Dictionary<string,FieldInfo> bindFieldMap = new Dictionary<string, FieldInfo>();
			Dictionary<string,FieldInfo> bindFieldMap2 = new Dictionary<string, FieldInfo>();
			
			foreach(FieldInfo field in fields)
			{
				// Bind対象じゃない場合
				if ( IsIgnoreField( field, _monoBehaviour, ignoreRuleMethod ) ){ continue; }
				
				// Bind名を取得
				string bindName = GetBindName( field );
				
				// Bind検索方向を取得
				bool searchParent = false;
				AutoBindAttribute attr = GetAutoBindAttribute( field );
				if ( attr!=null ){ searchParent = attr.searchParent; }
				
				// 検索方向別に格納
				if ( searchParent==false ){ bindFieldMap[ bindName ] = field; }
				else{ bindFieldMap2[ bindName ] = field;}
			}
			
			//Binding
			Transform root = _monoBehaviour.transform;
			Binding( _monoBehaviour, root, bindFieldMap );
			BindingSearchParent( _monoBehaviour, root, bindFieldMap2 );

#if UNITY_EDITOR
			//Binding Prefab
			Dictionary<string,FieldInfo> prefabFieldMap = new Dictionary<string, FieldInfo>();
			CreatePrefabFieldMap( bindFieldMap, ref prefabFieldMap );
			CreatePrefabFieldMap( bindFieldMap2, ref prefabFieldMap );
			BindingPrefab( _monoBehaviour, prefabFieldMap );
#endif

#if UNITY_EDITOR
			stw.Stop();
			Debug.Log(
				string.Format("{0} auto binding time {1}msec",_monoBehaviour.GetType().FullName,stw.ElapsedMilliseconds));
#endif
			
			// clear
			_monoBehaviour = null;
		}

		protected virtual void Binding(MonoBehaviour monoBehaviour,Transform root,Dictionary<string,FieldInfo> fieldDic)
		{
			if ( fieldDic.Count==0 ){ return;}
			if ( root.childCount==0 ){ return;}
			
			foreach(Transform child in root)
			{
				// 名前がマッチした場合
				string name = child.name.ToLower();
				FieldInfo field;
				if ( fieldDic.TryGetValue( name, out field ) )
				{
					// 値を設定
					object val = GetBindObject( child, field );
					// val==nullの場合でも設定
					field.SetValue( monoBehaviour, val );
					// 最初にマッチしたものだけ設定できる
					fieldDic.Remove( name );
				}
				// childにchildが存在する場合
				if ( child.childCount>0 )
				{
					Binding( monoBehaviour, child, fieldDic );
				}
			}
		}
		
		protected virtual void BindingSearchParent(MonoBehaviour monoBehaviour,Transform root,Dictionary<string,FieldInfo> fieldDic)
		{
			if ( fieldDic.Count==0 ){ return;}
			if ( root.parent==null ){ return;}
			
			Transform parent = root.parent;
			
			// 名前がマッチした場合
			string name = parent.name.ToLower();
			FieldInfo field;
			if ( fieldDic.TryGetValue( name, out field ) )
			{
				// 値を設定
				object val = GetBindObject( parent, field );
				// val==nullの場合でも設定
				field.SetValue( monoBehaviour, val );
				// 最初にマッチしたものだけ設定できる
				fieldDic.Remove( name );
			}
			// childにchildが存在する場合
			if ( parent.parent!=null )
			{
				BindingSearchParent( monoBehaviour, parent, fieldDic );
			}
		}
		
		//
		// Bindするobjectを取得する( GameObject or Component )
		//
		protected virtual object GetBindObject(Transform target,FieldInfo field)
		{
			object val = null;
			if ( field.FieldType==typeof(GameObject) )
			{
				val = target.gameObject;
			}
			else
			{
				val = target.GetComponent( field.FieldType );
			}
			return val;
		}

		//
		// PrefabをBindする
		//
		protected virtual void BindingPrefab(MonoBehaviour monoBehaviour,Dictionary<string,FieldInfo> prefabFieldDic)
		{
			// create PrefabFilePath Dictionary
			// key=Prefab Filename(remote extention) value= Prefab FilePath
			if ( _prefabPathMap==null )
			{
				_prefabPathMap = CreatePrefabAssetPathMap();
			}

			foreach(string fieldName in prefabFieldDic.Keys)
			{
				string path;
				// remove suffix "Prefab"
				string name = fieldName.Substring(0,fieldName.Length-6);
				if ( _prefabPathMap.TryGetValue( name, out path) )
				{
					FieldInfo field = prefabFieldDic[ fieldName ];
					// LoadAsset
					object val = AssetDatabase.LoadAssetAtPath( path, field.FieldType);
					// val==nullの場合でも設定
					field.SetValue( monoBehaviour, val );
				}
			}
		}

		protected virtual Dictionary<string,string> CreatePrefabAssetPathMap()
		{
			Dictionary<string,string> prefabPathMap = new Dictionary<string, string>();
			string[] paths = AssetDatabase.GetAllAssetPaths();
			foreach(string path in paths)
			{
				if ( path.EndsWith(".prefab") )
				{
					string fileName = System.IO.Path.GetFileNameWithoutExtension( path );
					prefabPathMap[ fileName.ToLower() ] = path;
				}
			}
			return prefabPathMap;
		}

		// PrefabをBind対象とするFieldInfoのDictionaryを作成する
		protected virtual void CreatePrefabFieldMap(Dictionary<string,FieldInfo> input,ref Dictionary<string,FieldInfo> output)
		{
			foreach(KeyValuePair<string,FieldInfo> kv in input)
			{
				if ( IsPrefabBindField( kv.Value.Name ) )
				{
					output[ kv.Key ] = kv.Value;
				}
			}
		}

		//
		// 指定フィールドがPrefabをBindするのかチェックする
		//
		protected virtual bool IsPrefabBindField(string fieldName)
		{
			// suffixが"Prefab"の場合はture
			return fieldName.EndsWith("Prefab");
		}
		
		//
		// Field情報を取得
		//
		protected virtual FieldInfo[] GetFields(MonoBehaviour monoBehaviour)
		{
			return monoBehaviour.GetType().GetFields(
				BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);			
		}
		
		//
		// Method情報を取得
		//
		protected virtual MethodInfo[] GetMethods(MonoBehaviour monoBehaviour)
		{
			return monoBehaviour.GetType().GetMethods(
				BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);	
		}
		
		//
		// Bindに使用する名称を取得
		//
		protected virtual string GetBindName(FieldInfo field)
		{
			// Attribute設定を優先
			AutoBindAttribute attr = GetAutoBindAttribute( field );
			if ( attr!=null && string.IsNullOrEmpty( attr.name )==false )
			{
				return attr.name;
			}
			
			// Field名先頭にアンダーバーがある場合は削除
			string bindName;
			if ( field.Name.StartsWith("_") && field.Name.Length>2 )
			{
				bindName = field.Name.Substring(1);
			}
			else
			{
				bindName = field.Name;
			}
			// 小文字に変換
			return bindName.ToLower();
		}
		
		//
		// Bind不要Fieldかチェック
		//
		protected virtual bool IsIgnoreField( FieldInfo field, MonoBehaviour monoBehaviour,MethodInfo ignoreRuleMethod )
		{
			// Bind対象はGameObject or Componentそれ以外は対象外
			if (  field.FieldType!=typeof(GameObject) && field.FieldType.IsSubclassOf(typeof(Component))==false )
			{
				return true;
			}
			
			// IgnoreBindAttribute check
			if ( GetIgnoreBindAttribute( field )!=null ){ return true; }
			
			// IgnoreRuleAtrtribute で指定されたMethodでIgnoreチェック
			if ( ignoreRuleMethod!=null )
			{
				bool ignore = (bool)ignoreRuleMethod.Invoke( monoBehaviour, new object[]{field.Name} );
				return ignore;			
			}
			
			return false;
		}
		
		//
		// Ignore判定をするMethod情報を取得
		//
		protected virtual MethodInfo GetIgnoreRuleMethod(MonoBehaviour monoBehaviour)
		{
			MethodInfo[] methods = GetMethods( monoBehaviour );			
			MethodInfo ignoreRuleMethod = null;
			
			foreach(MethodInfo method in methods)
			{
				if ( GetIgnoreRuleAttribute( method )!=null )
				{
					if ( method.ReturnType==typeof(bool)==false ){ continue; }					
					ignoreRuleMethod = method;
					break;
				}
			}			

			return ignoreRuleMethod;
		}		
		
		//
		// Bind対象のインスタンス取得
		//
		protected MonoBehaviour GetMonoBehaviour()
		{
			return _monoBehaviour;
		}
		
		//
		// AutoBindAttributeの取得
		//
		protected AutoBindAttribute GetAutoBindAttribute(MonoBehaviour monoBehaviour)
		{
			return GetAttribute<AutoBindAttribute>( monoBehaviour.GetType() );			
		}
		
		//
		// AutoBindAttributeの取得
		//		
		protected AutoBindAttribute GetAutoBindAttribute(FieldInfo field)
		{
			return GetAttribute<AutoBindAttribute>( field );
		}
		
		//
		// IgnoreBindAttributeの取得
		//		
		protected IgnoreBindAttribute GetIgnoreBindAttribute(FieldInfo field)
		{
			return GetAttribute<IgnoreBindAttribute>( field );
		}
		
		//
		// IgnoreRuleAttributeの取得
		//	
		protected IgnoreRuleAttribute GetIgnoreRuleAttribute(MethodInfo method)
		{
			return GetAttribute<IgnoreRuleAttribute>( method );
		}
		
		protected T GetAttribute<T>(MemberInfo memberInfo) where T:class
		{
			T[] attributes = memberInfo.GetCustomAttributes(typeof(T),true) as T[];
			
			if ( attributes==null || attributes.Length==0 ){ return null;}
			
			return attributes[0];
		}

	}

}