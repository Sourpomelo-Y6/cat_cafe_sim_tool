using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonCatVisibility : MonoBehaviour
    {
        public Toggle rosterToggle, preparationToggle;
        private bool showAdopted;private PythonCafeState current;private string instance;
        public bool ShowAdopted=>showAdopted;
        public PythonCatState[] Filter(PythonCatState[] cats)=>Array.FindAll(cats ?? new PythonCatState[0],cat=>showAdopted || cat.activity!="adopted");
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;showAdopted=false;}
            current=state;RefreshControls();
        }
        public void Change(bool value)
        {
            if(current==null)return;showAdopted=value;RefreshControls();
            GetComponent<ContentPreview>().RefreshCatVisibility();
            GetComponent<PythonPreparationPanel>()?.RefreshVisibility();
            GetComponent<PythonCatDetailsPanel>()?.RefreshVisibility();
        }
        public void RefreshControls()
        {
            if(rosterToggle!=null){rosterToggle.SetIsOnWithoutNotify(showAdopted);rosterToggle.interactable=current!=null;rosterToggle.gameObject.SetActive(current!=null && GetComponent<PythonServiceScreen>()?.ShowingSeats!=true);}
            if(preparationToggle!=null){preparationToggle.SetIsOnWithoutNotify(showAdopted);preparationToggle.interactable=current!=null;}
        }
    }
}
