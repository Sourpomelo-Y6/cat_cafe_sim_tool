using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    // Shared by the scene generator and runtime, so regenerated and existing scenes agree.
    public static class PlayerInteractionLayout
    {
        static void Place(Component c,float x,float y,float w,float h)
        {
            if(c==null)return;
            var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        public static void Apply(PythonPlayerPanel p)
        {
            var overlay=p.panel.GetComponent<RectTransform>();overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            var card=p.title.transform.parent.GetComponent<RectTransform>();
            card.anchorMin=card.anchorMax=card.pivot=new Vector2(.5f,.5f);card.anchoredPosition=Vector2.zero;card.sizeDelta=new Vector2(1248,768);
            p.panel.GetComponent<Image>().color=new Color(.10f,.14f,.12f,.98f);
            Place(p.title,20,10,790,36);p.title.fontSize=25;
            Place(p.summary,20,50,1208,102);p.summary.fontSize=16;
            Place(p.picture.transform.parent,20,160,792,400);
            Place(p.imageNotice,20,330,792,50);
            Place(p.choiceLabel,832,160,396,26);p.choiceLabel.fontSize=16;
            Place(p.targets,832,190,396,36);
            for(int i=0;i<p.actions.Length;i++)
            {
                Place(p.actions[i],832+(i%2)*202,238+(i/2)*68,194,36);
                p.actions[i].GetComponentInChildren<Text>().fontSize=16;
                if(p.actionHints!=null && i<p.actionHints.Length)
                {Place(p.actionHints[i],832+(i%2)*202,275+(i/2)*68,194,30);p.actionHints[i].resizeTextForBestFit=false;p.actionHints[i].fontSize=13;}
            }
            Place(p.resultGuide,832,160,396,306);
            Place(p.notice,832,512,396,50);p.notice.fontSize=14;
            Place(p.finish,832,570,396,40);Place(p.preparation,832,570,396,40);Place(p.back,832,620,396,44);
            if(p.preparation!=null)p.preparation.GetComponentInChildren<Text>().fontSize=16;
            var help=card.Find("Gauge Help");
            if(help==null){var label=Object.Instantiate(p.imageNotice,card);label.name="Gauge Help";help=label.transform;}
            var helpText=help.GetComponent<Text>();helpText.alignment=TextAnchor.UpperLeft;helpText.fontSize=14;
            var local=CafeBackends.Local as LocalCafeBackend;
            string threshold=local!=null ? local.Simulation.Rules.exchange.optional_threshold.ToString("0.#") : "条件値";
            helpText.text="関心：動作への集中。条件が揃うと心を開きます。\nテンション：交流の高まり。"+threshold+"以上で「心をつかむ」を選べます。不可理由は行動の下に表示します。";
            Place(help,832,680,396,70);
            var heading=card.Find("History Heading");
            if(heading!=null){Place(heading,20,568,510,28);heading.GetComponent<Text>().text="最新の描写 ／ 履歴は右のボタンから";heading.GetComponent<Text>().fontSize=15;}
            if(p.historyToggle==null)
            {
                p.historyToggle=Object.Instantiate(p.back,card);p.historyToggle.name="History Toggle";
                p.historyToggle.onClick=new Button.ButtonClickedEvent();p.historyToggle.onClick.AddListener(p.ToggleHistory);
            }
            p.historyToggle.onClick=new Button.ButtonClickedEvent();p.historyToggle.onClick.AddListener(p.ToggleHistory);
            p.historyToggle.gameObject.SetActive(true);p.historyToggle.interactable=true;Place(p.historyToggle,542,564,270,32);p.historyToggle.GetComponentInChildren<Text>().fontSize=16;
            Place(p.historyScroll,20,604,792,144);
            Place(p.historyScroll.viewport,8,6,760,132);p.history.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,760);
            var bar=p.historyScroll.verticalScrollbar;if(bar!=null)Place(bar,774,6,12,132);
        }
    }
}
