using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonPreparationRoster : MonoBehaviour
    {
        public RectTransform content;
        public GameObject template;
        public ScrollRect scroll,forecast,goal;
        public Button forecastTab,goalTab;
        private readonly List<GameObject> cards=new List<GameObject>();
        private bool showingGoal;
        public int Count=>Panel.Rows.Length;
        private PythonPreparationPanel Panel=>GetComponent<PythonPreparationPanel>();
        public Button WorkButton(int index)=>cards[index].transform.Find("Work").GetComponent<Button>();
        public Button RestButton(int index)=>cards[index].transform.Find("Rest").GetComponent<Button>();
        public Button SelectButton(int index)=>cards[index].transform.Find("Select").GetComponent<Button>();
        public string CardId(int index)=>cards[index].name;
        public void Forecast(){showingGoal=false;Refresh();Canvas.ForceUpdateCanvases();forecast.verticalNormalizedPosition=1;}
        public void Goal(){showingGoal=true;Refresh();Canvas.ForceUpdateCanvases();goal.verticalNormalizedPosition=1;}
        void Select(string id){Panel.SelectCatId(id);Forecast();}
        public void Refresh()
        {
            if(template==null || Panel.CurrentState==null)return;
            var rows=Panel.Rows;float position=scroll.verticalNormalizedPosition;
            while(cards.Count<rows.Length){var card=Instantiate(template,content);cards.Add(card);}
            for(int i=0;i<cards.Count;i++)
            {
                var card=cards[i];card.SetActive(i<rows.Length);if(i>=rows.Length)continue;
                var cat=rows[i];string id=cat.cat_id;card.name=id;var rect=card.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(0,-i*120);
                bool working=Panel.Working(cat);card.GetComponent<Image>().color=id==Panel.SelectedCatId?new Color(.76f,.87f,.73f):cat.activity!="cafe" || cat.health_status!="healthy"?new Color(1f,.94f,.86f):working?new Color(.91f,.97f,.9f):new Color(.92f,.95f,1f);
                var picture=card.transform.Find("Portrait/Picture").GetComponent<RawImage>();picture.texture=GetComponent<ContentPreview>().PythonCatTexture(cat);picture.enabled=picture.texture!=null;if(picture.texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
                card.transform.Find("Info").GetComponent<Text>().text=cat.name+"　"+(working?"出勤":cat.activity=="cafe"?"休養":"不在")+(Panel.HasProposal?"（案）":"")+"\n"+PythonCatCard.Health(cat)+" / "+PythonCatCard.Activity(cat,false)+"\n体力 "+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+"　疲労 "+cat.fatigue.ToString("0.#")+"　ストレス "+cat.stress.ToString("0.#");
                var select=SelectButton(i);select.onClick.RemoveAllListeners();select.onClick.AddListener(()=>Select(id));select.interactable=true;
                var work=WorkButton(i);var rest=RestButton(i);work.onClick.RemoveAllListeners();rest.onClick.RemoveAllListeners();work.onClick.AddListener(()=>Panel.ShiftCat(id,true));rest.onClick.AddListener(()=>Panel.ShiftCat(id,false));work.interactable=Panel.CanShift(id,true);rest.interactable=Panel.CanShift(id,false);
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,Math.Max(scroll.viewport.rect.height,rows.Length*120));scroll.verticalNormalizedPosition=position;
            forecast.gameObject.SetActive(!showingGoal);goal.gameObject.SetActive(showingGoal);forecastTab.GetComponent<Image>().color=!showingGoal?new Color(.76f,.87f,.73f):Color.white;goalTab.GetComponent<Image>().color=showingGoal?new Color(.76f,.87f,.73f):Color.white;
        }
    }
}
