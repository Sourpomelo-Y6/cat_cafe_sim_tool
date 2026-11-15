using CatCafePreview.Standalone;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public sealed class SatoEpiloguePanel : AcquisitionConversationPanel
    {
        private GameObject host;
        private PythonCatState cat;
        private CafeConversationProgress reading;
        private string instance, customer;

        public void OpenCat(PythonCatState value)
        {
            if(GetComponents<AcquisitionConversationPanel>().AnyShowing(this))return;
            cat=value;
            customer=Backend.Simulation.NamedEpilogueCustomer(cat.cat_id);
            instance=Client.CurrentState.instance_id;
            reading=new CafeConversationProgress { conversation_id=CafeSimulation.NamedEpilogueId(customer), branch_id="letter",
                viewed_branches=System.Array.Empty<string>() };
            var details=GetComponent<PythonCatDetailsPanel>();
            if(host==null)
            {
                host=new GameObject("Sato Adoption Epilogue",typeof(RectTransform),typeof(Image));
                host.transform.SetParent(details.panel.transform.parent,false);
                host.SetActive(false);
            }
            Bind(Client.CurrentState,host,details.title.font);
            Open();
            if(Showing)details.panel.SetActive(false);
        }

        protected override void Update()
        {
            if(Showing && Client.CurrentState?.instance_id!=instance){Close();return;}
            base.Update();
        }

        protected override CafeConversationProgress ResolveProgress()=>reading;
        protected override bool Available=>reading!=null && !reading.completed;
        protected override bool ThreeLinePages=>true;
        protected override bool TrackRead=>false;
        protected override void SetOpen(bool open){}
        protected override void RecordCompletion(){}
        protected override string Title=>Backend.Details.CustomerName(customer)+"からの便り";
        protected override string HistoryPrefix=>"";
        protected override string Notice=>"譲渡後の後日談";
        protected override string EndText=>"猫の詳細へ戻ります。";
        protected override PythonIntakeState Request=>new PythonIntakeState {cat_id=cat.content_cat_id,name=cat.name};

        protected override CafeConversationLine[] Lines(string branch)
            => CafeNamedDispatchDialogue.Epilogue(Backend.Simulation.Rules.named_dispatch,customer,cat.name,Backend.Details.CustomerName(customer));

        protected override void AfterClose()
        {
            if(Client.CurrentState?.instance_id==instance)
                GetComponent<PythonCatDetailsPanel>().Apply(Client.CurrentState);
            base.AfterClose();
        }

        protected override void Finish()
        {
            CafeMissingDialogue.Finish(reading);
            if(Backend?.Simulation.SatoEpilogueAvailable(cat.cat_id)==true)
                Backend.Simulation.NamedRequest(customer).epilogue_read=true;

        }
    }
}
