using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonCatCard : MonoBehaviour
    {
        public RawImage picture;
        public Text catName, stateText;
        public GameObject customerIcon;
        public Button work, rest, details;
        [NonSerialized] public PythonCatState state;
        public void Set(PythonCatState cat, PythonCafeState cafe, ContentPreview preview, PythonStateClient client)
        {
            state = cat; catName.text = cat.name;
            picture.texture = preview.PythonCatTexture(cat);
            picture.enabled = picture.texture != null;
            bool occupied = cafe.seats != null && Array.Exists(cafe.seats,
                seat => seat.cat_id == cat.cat_id && !string.IsNullOrEmpty(seat.customer_id));
            customerIcon.SetActive(occupied);
            string activity=Activity(cat,occupied);
            stateText.text=activity+" / "+Health(cat)+"\n体力 "+cat.stamina.ToString("0.#")+" / "+cat.max_stamina.ToString("0.#")+"\n疲労 "+cat.fatigue.ToString("0.#")+" / ストレス "+cat.stress.ToString("0.#");
            var background=GetComponent<Image>();if(background!=null)background.color=cat.activity!="cafe" || cat.health_status!="healthy"?new Color(1f,.94f,.86f):cat.working?new Color(.91f,.97f,.9f):new Color(.92f,.95f,1f);
            stateText.color=new Color(.16f,.27f,.23f);
            work.onClick.RemoveAllListeners(); rest.onClick.RemoveAllListeners();
            work.onClick.AddListener(() => client.SetCatShift(cat.cat_id, true));
            rest.onClick.AddListener(() => client.SetCatShift(cat.cat_id, false));
            if(details != null)
            {
                details.onClick.RemoveAllListeners();
                details.onClick.AddListener(() => preview.GetComponent<PythonCatDetailsPanel>().OpenFromList(cat.cat_id,false));
            }
            Refresh(client);
        }
        public static string Health(PythonCatState cat)=>string.IsNullOrEmpty(cat.health_label)?cat.health_status=="healthy"?"健康":cat.health_status:cat.health_label;
        public static string Activity(PythonCatState cat,bool occupied)
        {return cat.activity!="cafe"?(string.IsNullOrEmpty(cat.activity_label)?cat.activity:cat.activity_label):cat.health_status!="healthy"?"療養":occupied?"接客中":cat.working?"出勤・待機":"休養";}
        public void Refresh(PythonStateClient client)
        {
            if(details != null) details.interactable=client != null && client.CanManageSaves && state != null;
            bool ready = client != null && client.CanChangeShifts && state != null;
            work.interactable = ready && !state.working && state.health_status == "healthy" && state.activity == "cafe";
            rest.interactable = ready && state.working;
        }
    }
}
