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
            string activity = cat.activity!="cafe" ? cat.activity_label : cat.health_status!="healthy" ? "療養" : occupied ? "接客中" : cat.working ? "出勤・待機" : "休養";
            if(string.IsNullOrEmpty(activity))activity=cat.activity;
            stateText.text = activity+" / "+(cat.health_label ?? cat.health_status)+"\n体力 "+cat.stamina.ToString("0.#")+"/"+cat.max_stamina.ToString("0.#")+"　疲労 "+cat.fatigue.ToString("0.#");
            stateText.color=cat.activity!="cafe" || cat.health_status!="healthy" ? new Color(.55f,.27f,.13f) : new Color(.16f,.27f,.23f);
            work.onClick.RemoveAllListeners(); rest.onClick.RemoveAllListeners();
            work.onClick.AddListener(() => client.SetCatShift(cat.cat_id, true));
            rest.onClick.AddListener(() => client.SetCatShift(cat.cat_id, false));
            if(details != null)
            {
                details.onClick.RemoveAllListeners();
                details.onClick.AddListener(() => preview.GetComponent<PythonCatDetailsPanel>().Open(cat.cat_id));
            }
            Refresh(client);
        }
        public void Refresh(PythonStateClient client)
        {
            if(details != null) details.interactable=client != null && client.CanManageSaves && state != null;
            bool ready = client != null && client.CanChangeShifts && state != null;
            work.interactable = ready && !state.working && state.health_status == "healthy" && state.activity == "cafe";
            rest.interactable = ready && state.working;
        }
    }
}
