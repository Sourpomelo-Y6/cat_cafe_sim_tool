using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonAssignmentMatch {public string cat_id,details;}
    [Serializable] public class PythonAssignmentCustomer {public string customer_id,name,details;public PythonAssignmentMatch[] matches;}
    [Serializable] public class PythonAssignmentCat {public string cat_id,name,reason,details;public bool available;}
    [Serializable] public class PythonAssignmentSeat {public string seat_id,label,reason,details;public bool available;}
    [Serializable] public class PythonServiceAssignment {public bool auto_assign,waiting,can_assign;public string reason,notes;public PythonAssignmentCustomer[] customers;public PythonAssignmentCat[] cats;public PythonAssignmentSeat[] seats;}
    public class PythonServiceAssignmentPanel : MonoBehaviour
    {
        public GameObject panel;public Dropdown customers,cats,seats;public Text title,details,notice;public Button mode,proceed,close;
        private PythonCafeState current;private string instance,customerId,catId,seatId,message="";private bool online,wasWaiting;
        private PythonServiceAssignment View=>current?.service_assignment;
        private PythonAssignmentCustomer Customer=>Array.Find(View?.customers ?? new PythonAssignmentCustomer[0],r=>r.customer_id==customerId);
        private PythonAssignmentCat Cat=>Array.Find(View?.cats ?? new PythonAssignmentCat[0],r=>r.cat_id==catId);
        private PythonAssignmentSeat Seat=>Array.Find(View?.seats ?? new PythonAssignmentSeat[0],r=>r.seat_id==seatId);
        public void Open(){var client=GetComponent<PythonStateClient>();if(!online || View==null || !client.CanManageSaves)return;if(client.Automatic)client.ToggleAutomatic();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();}
        public void Close(){panel.SetActive(false);}
        public void ResetSelection(){customerId=catId=seatId=null;message="";wasWaiting=false;Close();}
        public void SelectCustomer(int i){if(!Ready || i<0 || i>=(View?.customers?.Length ?? 0))return;customerId=View.customers[i].customer_id;message="";Render();}
        public void SelectCat(int i){if(!Ready || i<0 || i>=(View?.cats?.Length ?? 0))return;catId=View.cats[i].cat_id;message="";Render();}
        public void SelectSeat(int i){if(!Ready || i<0 || i>=(View?.seats?.Length ?? 0))return;seatId=View.seats[i].seat_id;message="";Render();}
        private bool Ready=>online && GetComponent<PythonStateClient>().CanManageSaves;
        public void ToggleMode(){if(online)GetComponent<PythonStateClient>().ToggleAutoAssignment();}
        public void Proceed(){if(online)GetComponent<PythonStateClient>().AssignService(customerId,catId,seatId);}
        public void Notify(string text){message=text;RefreshControls();}
        public void Apply(PythonCafeState state){if(instance!=state.instance_id){instance=state.instance_id;ResetSelection();}current=state;online=true;Render();bool waiting=View?.waiting==true;if(waiting && !wasWaiting && Ready)Open();if(Ready || !waiting)wasWaiting=waiting;}
        static void Fill(Dropdown d,string[] labels,int index){d.options.Clear();foreach(var label in labels)d.options.Add(new Dropdown.OptionData(label));if(labels.Length==0)d.options.Add(new Dropdown.OptionData("対象なし"));d.SetValueWithoutNotify(Math.Max(0,index));d.RefreshShownValue();}
        private void Render()
        {
            var customersList=View?.customers ?? new PythonAssignmentCustomer[0];var catList=View?.cats ?? new PythonAssignmentCat[0];var seatList=View?.seats ?? new PythonAssignmentSeat[0];
            if(!Array.Exists(customersList,r=>r.customer_id==customerId))customerId=customersList.FirstOrDefault()?.customer_id;
            if(!Array.Exists(catList,r=>r.cat_id==catId))catId=(catList.FirstOrDefault(r=>r.available) ?? catList.FirstOrDefault())?.cat_id;
            if(!Array.Exists(seatList,r=>r.seat_id==seatId))seatId=(seatList.FirstOrDefault(r=>r.available) ?? seatList.FirstOrDefault())?.seat_id;
            Fill(customers,customersList.Select(r=>r.name).ToArray(),Array.FindIndex(customersList,r=>r.customer_id==customerId));Fill(cats,catList.Select(r=>r.name+(r.available ? "（担当可能）" : "（条件を確認）")).ToArray(),Array.FindIndex(catList,r=>r.cat_id==catId));Fill(seats,seatList.Select(r=>r.label+(r.available ? "（空席）" : "（使用中・不可）")).ToArray(),Array.FindIndex(seatList,r=>r.seat_id==seatId));
            title.text="接客の割り当て / 自動："+(View?.auto_assign==false ? "OFF" : "ON");
            var match=Array.Find(Customer?.matches ?? new PythonAssignmentMatch[0],r=>r.cat_id==catId);
            details.text=(Customer==null ? "待機中のお客さんはいません。" : "選んだ客："+Customer.name+" / "+Customer.details)+"\n\n"+(Cat?.details ?? "猫がいません。")+(Cat?.available==false ? "\n"+Cat.reason : "")+"\n"+(match?.details ?? "")+"\n\n選んだ席："+(Seat?.label ?? "対象なし")+"\n"+(Seat?.details ?? "")+"\n\n全席の状況\n"+string.Join("\n",seatList.Select(r=>r.label+" / "+r.details));RefreshControls();
        }
        public void RefreshControls(){var client=GetComponent<PythonStateClient>();customers.interactable=Ready && (View?.customers?.Length ?? 0)>0;cats.interactable=Ready && (View?.cats?.Length ?? 0)>0;seats.interactable=Ready && (View?.seats?.Length ?? 0)>0;mode.interactable=online && client.CanChangeAutoAssignment;mode.GetComponentInChildren<Text>().text=View?.auto_assign==false ? "自動割り当てをON" : "自動割り当てをOFF";proceed.interactable=online && client.CanAssignService(customerId,catId,seatId);close.interactable=true;notice.text=!online ? "未接続です。再接続後に操作できます。" : !Ready ? "操作を送信しています。" : (message!="" ? message : View?.reason ?? "")+"\n"+(View?.notes ?? "");}
        public void Disconnect(){online=false;RefreshControls();}
    }
}
