using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Asset;
using UnityEngine.UI;
using QTool;
using QTool.Inspector;
using UnityEngine.SceneManagement;
#if QTest
using UnityEngine.AddressableAssets;
#endif
public class ResourceTest : QAssetLoader<ResourceTest,Sprite>
{

}

public class AssetTest : MonoBehaviour
{
    public Text text;
	public Image sView;
	public static Sprite sprite;
	public static GameObject obj;
	public static List<Sprite> objList = new List<Sprite>();
	public bool isresources = false;
	public async void ResourceTestLoad()
	{
		if (isresources)
		{
			obj = Resources.Load<GameObject>(nameof(ResourceTest) + "/" + nameof(ResourceTest) + nameof(GameObject));
			sprite = Resources.Load<Sprite>(nameof(ResourceTest) + "/" + nameof(ResourceTest) + nameof(Sprite));
		}
		else
		{
		}

		sView.sprite = sprite;
		Instantiate(obj, transform);
		objList.Clear();
		objList.AddRange(ResourceTest.LoadAll());
		Debug.LogError(obj + " : " + sprite+"\n"+objList.ToOneString());
	}
	public void ResourceTestUnLoad()
	{
		if (isresources)
		{
			Resources.UnloadAsset(sprite);
			obj = null;
			objList.Clear();
		}
		else
		{
			obj = null;
			sView.sprite = null;
			sprite = null;
			objList.Clear();
		}
		
		
		Debug.LogError(obj + " : " + sprite);
	}
	public void GC()
	{
		Resources.UnloadUnusedAssets();
		System.GC.Collect();
	}
	public void LoadScene()
	{
		SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
	}
	// Start is called before the first frame update
	[ContextMenu("加载Test1")]
    async void LoadTest1()
    {
        //   Debug.LogError( await ResourceTest.GetAsync("test1"));
         var obj=ResourceTest.Load("Test1");
        text.text = "加载完成:" + obj;
    }
    [ContextMenu("加载全部")]
    void LoadAll()
    {
		//   Debug.LogError( await ResourceTest.GetAsync("test1"));
		objList.AddRange( ResourceTest.LoadAll());
        //text.text = "加载完成:" + ResourceTest.objDic.Count + ResourceTest.objDic.ToOneString();
    }
	[ContextMenu("GCLoading")]
	public async void GCLoading()
	{
		await Tool.LoadSceneAsync(SceneManager.GetActiveScene().name);
	}
	// Update is called once per frame
	void Update()
    {
        
    }
}
