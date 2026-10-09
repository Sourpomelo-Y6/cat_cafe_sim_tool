using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCatRosterLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Style(PythonCatCard card){card.GetComponent<RectTransform>().sizeDelta=new Vector2(242,310);Place(card.catName,10,6,185,30);Place(card.picture,61,42,120,116);Place(card.stateText,10,166,222,72);card.stateText.fontSize=15;card.stateText.resizeTextForBestFit=false;card.stateText.verticalOverflow=VerticalWrapMode.Truncate;Place(card.work,10,242,106,30);Place(card.rest,126,242,106,30);if(card.details!=null){Place(card.details,10,276,222,28);card.details.GetComponent<Image>().color=new Color(.88f,.93f,.86f);var label=card.details.GetComponentInChildren<Text>();Place(label,0,0,222,28);label.text="詳細・交流・ケア";label.alignment=TextAnchor.MiddleCenter;label.fontSize=14;}}
        [MenuItem("Cat Cafe/開発用/UI再生成/猫一覧と詳細の整理",false,23)]
        public static void Build()
        {
            PythonCatDetailsLayout.Build();var scene=EditorSceneManager.GetActiveScene();var preview=Object.FindObjectOfType<ContentPreview>();var roster=preview.GetComponent<PythonCatRoster>();var grid=roster.content.GetComponent<GridLayoutGroup>();grid.cellSize=new Vector2(242,310);
            string path=AssetDatabase.GetAssetPath(roster.cardPrefab);var prefab=PrefabUtility.LoadPrefabContents(path);
            try{Style(prefab.GetComponent<PythonCatCard>());PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var card in roster.content.GetComponentsInChildren<PythonCatCard>(true))Style(card);
            var scroll=roster.content.GetComponentInParent<ScrollRect>();scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_CAT_ROSTER_LAYOUT_OK");
        }
    }
}
