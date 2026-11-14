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
            reading=new CafeConversationProgress { conversation_id=customer==CafeSimulation.Suzuki?"suzuki.epilogue.v1":"sato.epilogue.v1", branch_id="letter",
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
        protected override string Title=>CafeSimulation.NamedCustomerName(customer)+"からの便り";
        protected override string HistoryPrefix=>"";
        protected override string Notice=>"譲渡後の後日談";
        protected override string EndText=>"猫の詳細へ戻ります。";
        protected override PythonIntakeState Request=>new PythonIntakeState {cat_id=cat.content_cat_id,name=cat.name};

        protected override CafeConversationLine[] Lines(string branch)=>customer==CafeSimulation.Suzuki?new[]{
            new CafeConversationLine("","鈴木さんから、"+cat.name+"の近況を知らせる手紙が届いた。"),
            new CafeConversationLine("鈴木さん","ねこじゃらしは棚に置いています。この前は"+cat.name+"が棚の下で待っていたので、そこに座って少し遊びました。"),
            new CafeConversationLine("鈴木さん","遊びたいときは、自分から来てくれますね。")
        }:new[]{
            new CafeConversationLine("","佐藤さんから、"+cat.name+"の近況を知らせる手紙が届いた。"),
            new CafeConversationLine("佐藤さん","窓辺にクッションを置きました。昨日はそこで午後の日差しを浴びていました。"),
            new CafeConversationLine("佐藤さん","店で隣に来てくれたときも、私が呼ぶより先に、あの子が自分で来てくれたんですよね。ここでも、"+cat.name+"が自分で選べる場所を増やしていきます。")
        };

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
