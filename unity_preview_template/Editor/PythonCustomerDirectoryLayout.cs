using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCustomerDirectoryLayout
    {
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Button Button(Transform parent,string name,string caption,float x,float y,float w)
        {
            var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name=name;g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,36);
            var t=g.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=15;t.text=caption;t.supportRichText=false;return g.GetComponent<Button>();
        }
        public static void RefreshIfReady(ContentPreview preview)
        {
            var p=preview.GetComponent<PythonCustomerDirectoryPanel>();if(p==null || p.panel==null)return;var card=p.close.transform.parent;
            p.filters=new Button[3];var labels=new[]{"全員","来店対象","要確認"};for(int i=0;i<3;i++){p.filters[i]=Button(card,"Directory Filter "+i,labels[i],24+i*112,130,108);UnityEventTools.AddIntPersistentListener(p.filters[i].onClick,p.FilterBy,i);}
            p.tabs=new Button[4];labels=new[]{"概要・予定","好み・評価","状態・信頼","猫との相性"};for(int i=0;i<4;i++){p.tabs[i]=Button(card,"Directory Tab "+i,labels[i],372+i*138,130,130);UnityEventTools.AddIntPersistentListener(p.tabs[i].onClick,p.SectionBy,i);}
            var list=p.listContent.GetComponentInParent<ScrollRect>(true);Place(list.GetComponent<RectTransform>(),24,176,332,326);Place(list.viewport,0,0,312,326);Place(list.verticalScrollbar.GetComponent<RectTransform>(),318,0,12,326);
            var detail=p.details.GetComponentInParent<ScrollRect>(true);Place(detail.GetComponent<RectTransform>(),372,176,544,326);Place(detail.viewport,6,6,514,314);Place(detail.verticalScrollbar.GetComponent<RectTransform>(),526,6,12,314);
            Place(p.adopted.GetComponent<RectTransform>(),372,508,260,34);p.adopted.GetComponentInChildren<Text>().fontSize=16;
            p.sort=Button(card,"Directory Sort","並び：登録順",644,508,272);UnityEventTools.AddPersistentListener(p.sort.onClick,p.CycleSort);
            Place(p.notice.rectTransform,24,508,332,38);p.notice.fontSize=13;
            p.panel.SetActive(false);
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/顧客名簿の整理")]
        public static void Build(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");RefreshIfReady(Object.FindObjectOfType<ContentPreview>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
    }
}
