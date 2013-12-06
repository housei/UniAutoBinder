UniAutoBinder
=============


UnityのInspectorで設定しているGameObject，TransformなどのComponent、Prefabを
命名規則によって自動設定することができます。

### 導入手順
Assets/UniAutoBinderフォルダーをプロジェクトにドラッグ＆ドロップ


### 命名規則による自動設定

```csharp
using UnityEngine;
using System.Collections;
using UniAutoBinder;

// AutoBind Attributeをつけると自動設定対象になる
[AutoBind]
public class Test : MonoBehaviour {

	// 自動設定の検索はTransformの子方向に検索し
	// Field名とGameObject名が同じ場合(大文字小文字を区別しない) 設定される
	public GameObject child1;
	// Prefixのアンダーバーは比較時は無視する
	public Transform _child2;	
	public MyComponent _child3;
	
	// privateでも設定するけど、SerializeField Attributeがついていないと意味ないね
	[SerializeField] 
	private BoxCollider _child4;

	// 別名でのマッチも可能
	[AutoBind("child5")]
	public GameObject _go;

	// 親方向へ検索して自動設定も可能
	[AutoBind(searchParent=true)]
	public GameObject _root;

	// 親方向検索かつ別名マッチ
	[AutoBind(name="parent",searchParent=true)]
	public GameObject _base;

	// Field名のSuffixがPrefabになっているものはPrefabを自動設定する
	// Project TreeからSuffixを除外した同名のPrefabを設定する( 大文字小文字の区別はしない ）
	// この場合はtestになる
	public GameObject testPrefab;

	[AutoBind("dogPrefab")]
	public GameObject _animalPrefab;

	[SerializeField] 
	private GameObject _test3Prefab;

	// 自動設定させたくない場合は IgnoreBind Attributeをつける
	[IgnoreBind]
	public GameObject _child5;

	public GameObject foobar1;
	public GameObject foobar2;
	public GameObject foobar3;

	// 自動設定させたくないFieldが大量にあるなどの場合は
	// IgnoreRuleMethod Attributeをつけた メソッドで対応もできる
	[IgnoreRuleMethod]
	public bool IsIgnoreField(string fieldName)
	{
		// prefixが foobarの場合は自動設定させない
		return fieldName.StartsWith("foobar");		
	}
	
}

```
### License
[The MIT License (MIT)](https://github.com/housei/UniAutoBinder/blob/master/LICENSE "The MIT License (MIT)")
