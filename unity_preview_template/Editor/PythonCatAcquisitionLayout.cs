using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCatAcquisitionLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Remove(Transform parent,string name){var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
        static Text Summary(Text source){var parent=source.transform.parent;Remove(parent,"Candidate Summary");var t=Object.Instantiate(source,parent);t.name="Candidate Summary";Place(t,24,110,892,74);t.fontSize=17;t.text="費用・資金・飼育枠";return t;}
        static Button Entry(Button source,Transform parent,string name,string caption){Remove(parent,name);var b=Object.Instantiate(source,parent);b.name=name;b.onClick=new Button.ButtonClickedEvent();b.GetComponentInChildren<Text>().text=caption;b.GetComponentInChildren<Text>().fontSize=16;return b;}
        static void Bind(Button b,UnityEngine.Events.UnityAction call){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,call);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        static void Portrait(RawImage picture,float x,float y,float w,float h){Place(picture.transform.parent.GetComponent<RectTransform>(),x,y,w,h);var r=picture.rectTransform;r.pivot=new Vector2(.5f,.5f);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Scroll(Text t,float x,float y,float w,float h){var scroll=t.GetComponentInParent<ScrollRect>(true);Place(scroll,x,y,w,h);Place(scroll.viewport,8,6,w-32,h-12);Place(scroll.verticalScrollbar,w-18,6,12,h-12);t.rectTransform.sizeDelta=new Vector2(w-32,t.rectTransform.sizeDelta.y);}
        static T Copy<T>(T source,Transform root,Transform target) where T:Component=>target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();
        public static void RefreshIfReady(){var p=Object.FindObjectOfType<ContentPreview>();if(p?.GetComponent<PythonRecruitmentPanel>()?.panel!=null && p.GetComponent<PythonDispatchIntroductionPanel>()?.panel!=null)Build();}
        [MenuItem("Cat Cafe/開発用/UI再生成/猫を迎える画面の整理",false,39)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=Object.FindObjectOfType<ContentPreview>();
            var n=p.GetComponent<PythonCatAcquisitionNavigation>() ?? p.gameObject.AddComponent<PythonCatAcquisitionNavigation>();
            var r=p.GetComponent<PythonRecruitmentPanel>();var s=p.GetComponent<PythonPetShopPanel>();var a=p.GetComponent<PythonRegularIntroductionPanel>();var d=p.GetComponent<PythonDispatchIntroductionPanel>();var v=p.GetComponent<PythonVisitingCatPanel>();var q=p.GetComponent<PythonIntakePanel>();
            n.panels=new[]{r.panel,s.panel,a.panel,d.panel,v.panel,q.panel};n.summaries=new Text[6];n.detailEntries=new Button[6];
            var titles=new[]{r.title,s.title,a.title,d.title,v.title};var pictures=new[]{r.picture,s.picture,a.picture,d.picture,v.picture};var details=new[]{r.details,s.details,a.details,d.details,v.details};var notices=new[]{r.notice,s.notice,a.notice,d.notice,v.notice};var housing=new[]{r.housing,s.housing,a.housing,d.housing,v.housing};var close=new[]{r.close,s.close,a.close,d.close,v.close};var confirmations=new[]{r.confirmation,s.confirmation,a.confirmation,d.confirmation,v.confirmation};
            for(int i=0;i<5;i++)
            {
                var card=titles[i].transform.parent;Place(titles[i],24,16,892,40);n.summaries[i]=Summary(notices[i]);Portrait(pictures[i],24,192,272,230);Scroll(details[i],316,192,600,230);Place(notices[i],24,484,892,58);notices[i].fontSize=15;
                Place(housing[i],24,550,284,40);Place(close[i],616,550,300,40);var entry=Entry(close[i],card,"Candidate Details Entry","迎えた猫の詳細");Place(entry,320,550,284,40);n.detailEntries[i]=entry;entry.gameObject.SetActive(false);confirmations[i].transform.SetAsLastSibling();confirmations[i].SetActive(false);n.panels[i].SetActive(false);
            }
            Place(r.trait,24,64,280,36);Place(r.coat,316,64,290,36);Place(r.hair,626,64,290,36);Portrait(r.picture,24,192,272,166);Place(r.accepted,24,364,272,28);Place(r.reset,24,398,272,28);Place(d.records,24,64,892,36);Place(d.records.captionText,12,4,844,28);d.records.captionText.fontSize=16;d.records.captionText.resizeTextForBestFit=true;d.records.captionText.resizeTextMinSize=12;d.records.captionText.resizeTextMaxSize=16;
            UnityEventTools.AddIntPersistentListener(n.detailEntries[0].onClick,n.OpenDetails,0);UnityEventTools.AddIntPersistentListener(n.detailEntries[1].onClick,n.OpenDetails,1);UnityEventTools.AddIntPersistentListener(n.detailEntries[2].onClick,n.OpenDetails,2);UnityEventTools.AddIntPersistentListener(n.detailEntries[3].onClick,n.OpenDetails,3);UnityEventTools.AddIntPersistentListener(n.detailEntries[4].onClick,n.OpenDetails,4);
            var qcard=q.title.transform.parent;Place(q.title,24,16,892,40);n.summaries[5]=Summary(q.title);Place(n.summaries[5],24,64,892,80);Portrait(q.picture,24,152,304,130);Scroll(q.details,24,290,304,192);Place(q.accept,348,152,568,30);Place(q.decline,348,318,568,30);Scroll(q.choiceDetails[0],348,188,568,122);Scroll(q.choiceDetails[1],348,354,568,128);
            q.housing=Entry(s.housing,qcard,"Candidate Housing","飼育スペースを確認");Place(q.housing,24,550,432,40);Bind(q.housing,q.Housing);n.detailEntries[5]=Entry(s.close,qcard,"Candidate Details Entry","迎えた猫の詳細");Place(n.detailEntries[5],480,550,436,40);UnityEventTools.AddIntPersistentListener(n.detailEntries[5].onClick,n.OpenDetails,5);n.detailEntries[5].gameObject.SetActive(false);for(int i=0;i<6;i++)n.detailEntries[i].onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            Remove(qcard,"Intake Confirmation");q.confirmation=Object.Instantiate(s.confirmation,qcard);q.confirmation.name="Intake Confirmation";q.confirmationText=Copy(s.confirmationText,s.confirmation.transform,q.confirmation.transform);q.confirm=Copy(s.confirm,s.confirmation.transform,q.confirmation.transform);q.cancel=Copy(s.cancel,s.confirmation.transform,q.confirmation.transform);Bind(q.confirm,q.Confirm);Bind(q.cancel,q.Cancel);Bind(q.accept,q.Accept);Bind(q.decline,q.Decline);q.confirmation.SetActive(false);q.panel.SetActive(false);
            var h=p.GetComponent<PythonHousingPanel>();Bind(h.close,h.Close);UnityEventTools.AddPersistentListener(h.close.onClick,n.ReturnHousing);
            var background=s.panel.transform.parent;n.joinedEntry=Entry(s.close,background,"Joined Cat Entry","迎えた猫の詳細");Place(n.joinedEntry,480,96,296,30);Bind(n.joinedEntry,n.OpenJoined);n.joinedEntry.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_ACQUISITION_LAYOUT_OK");
        }
    }
}
