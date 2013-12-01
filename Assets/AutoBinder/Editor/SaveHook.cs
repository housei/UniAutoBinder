using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using UniAutoBinder;

namespace UniAutoBinder
{	
	public class SaveHook : AssetModificationProcessor
	{
		private static IAutoBinder _autoBinder = new DefaultAutoBinder();

		// Save実行時に呼び出せれる
		static string[] OnWillSaveAssets(string[] paths)
		{
			// AutoBind対象のコンポーネントを取得
			List<MonoBehaviour> targets = GetAutoBindTargets();

			//
			IAutoBinder autoBinder = new DefaultAutoBinder();

			// Bind実行
			foreach(MonoBehaviour target in targets)
			{
				autoBinder.Bind( target );
			}

			return paths;
		}

		// シーン中のAutoBindアトリビュートがついたコンポーネントを取得
	 	static List<MonoBehaviour> GetAutoBindTargets()
		{
			// シーン中のすべてのコンポーネントを取得
			MonoBehaviour[] objs = GameObject.FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

			List<MonoBehaviour> list = new List<MonoBehaviour>();
			foreach(MonoBehaviour mono in objs)
			{
				// AutoBindがついたMonoBehaviourのみを選別
				AutoBindAttribute[] attrs =
					mono.GetType().GetCustomAttributes(typeof(AutoBindAttribute),true) as AutoBindAttribute[];
				if (attrs!=null && attrs.Length>0 )
				{
					Debug.Log("bind target "+mono);
					list.Add(mono);
				}
			}

			return list;
		}
	}
}