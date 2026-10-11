using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonPatronDispatch GeneralDispatchView()
        {
            var s=Simulation.State;
            return new PythonPatronDispatch {options=s.cats.SelectMany(c=>Simulation.DispatchDestinations().Where(r=>!Simulation.IsPatronDestination(r.id)).Select(r=>new PythonDispatchOption {cat_id=c.id,choice=r.id,label=Simulation.Definition(c.id).name+" / "+r.name,days=r.days,reward=(float)Simulation.DispatchReward(c.id,r.id),detail=(r.id=="mountain_lodge_visit"?"家出確率 "+Number(Simulation.DispatchTroubleProbability(c.id)*100)+"％（出発時固定） / 捜索費 "+Number(Simulation.Rules.dispatch_trouble.search_cost)+" / 中断は報酬・派遣経験なし / ":"")+"疲労 "+r.max_fatigue+"以下 / 出発費用なし / 必要特性："+(string.IsNullOrEmpty(r.required_trait_name)?"指定なし":r.required_trait_name)+(r.item_reward==null || string.IsNullOrEmpty(r.item_reward.id)?"":" / 用品："+r.item_reward.name)+" / 選択前の報酬見込み",reason=Simulation.DispatchReason(c.id,r.id),can_select=Simulation.DispatchReason(c.id,r.id)==""})).ToArray(),events=s.dispatches.Where(e=>!Simulation.IsPatronDestination(e.destination)).Select(e=>new PythonDispatchEvent {cat_id=e.cat_id,choice=e.id,label=Simulation.Definition(e.cat_id).name+" / "+Simulation.DispatchDestination(e.destination).name,status=e.status,remaining=e.remaining,reward=(float)Simulation.DispatchReceivedReward(e),needs_response=e.status=="waiting",can_select=e.status=="waiting" && Simulation.DispatchReceiveReason(e.id)=="",reason=e.status=="resolved"?"受け取り済みです。":Simulation.DispatchReceiveReason(e.id),detail="出発 "+e.started_day+"日目 / 残り "+e.remaining+"日 / 報酬 "+Number(Simulation.DispatchReceivedReward(e))+(Standalone.CafeSimulation.DispatchInterrupted(e)?" / 帰還ストレス "+Number(Simulation.Rules.dispatch_trouble.return_stress)+"に設定・体力回復":" / 帰還ストレス ＋"+Number(e.return_stress))}).ToArray()};
        }
        private PythonDispatchTrouble[] DispatchTroublesView()=>Simulation.State.dispatches.Where(e=>e.trouble!=null).Select(e=>
        {
            var v=e.trouble;var r=Simulation.Rules.dispatch_trouble;
            return new PythonDispatchTrouble {event_id=e.id,cat_id=e.cat_id,label=Simulation.Definition(e.cat_id).name+" / 山あいの宿",status=v.status,activity_status=e.status,missing_day=v.missing_day,choice_day=v.choice_day,choice=v.choice,cost=(float)v.cost,return_reward=(float)Simulation.DispatchReceivedReward(e),description="出発時ストレス "+Number(v.stress)+" / 家出確率 "+Number(v.probability*100)+"％ / 残り "+e.remaining+"日 / 中断後は報酬・派遣経験なし、帰還時に体力回復・ストレス "+Number(r.return_stress),choices=new[]{"search","wait"}.Select(choice=>new PythonDispatchTroubleOption {choice=choice,label=choice=="search"?"費用を払って捜索":"費用なしで帰還を待つ",cost=(float)(choice=="search"?r.search_cost:0),funds_after=(float)(Simulation.State.funds-(choice=="search"?r.search_cost:0)),remaining=choice=="search"?0:r.missing_days,description=choice=="search"?"回答後すぐに帰還確認できます。":"家出当日を除く2日間を待って帰還を確認します。",reason=Simulation.DispatchTroubleReason(e.id,choice),can_select=v.status=="waiting" && Simulation.DispatchTroubleReason(e.id,choice)==""}).ToArray()};
        }).ToArray();
        private PythonDispatchIntroduction[] DispatchIntroductionsView()=>Simulation.State.dispatches.Where(e=>e.introduction!=null).Select(e=>
        {
            var v=e.introduction;var r=Simulation.DispatchIntroductionRule(e.destination).candidate;string reason=Simulation.DispatchIntroductionResponseReason(e.id),accept=Simulation.DispatchIntroductionAcceptReason(e.id);
            return new PythonDispatchIntroduction {event_id=e.id,destination=Simulation.DispatchDestination(e.destination).name,sender_name=Simulation.Definition(e.cat_id).name,cat_id=v.cat_id,name=v.name,status=v.status,cost=(float)v.cost,funds_after=(float)(Simulation.State.funds-v.cost),personality=r.personality,features=r.features_label,trait=r.trait,housing="在籍 "+Simulation.State.cats.Count+" / 上限 "+Simulation.HousingCapacity(Simulation.State,Simulation.State.day),presented_day=v.presented_day,resolved_day=v.resolved_day,response_reason=reason,accept_reason=accept,can_accept=accept=="",can_decline=reason==""};
        }).ToArray();
        private PythonDispatchEncounter[] DispatchChoicesView()=>Simulation.State.dispatches.Where(e=>e.encounter!=null && e.encounter.status!="scheduled").Select(e=>
        {
            var v=e.encounter;var r=Simulation.DispatchEncounterDefinition(e.destination,e.started_day);var cat=Simulation.State.cats.Single(c=>c.id==e.cat_id);
            return new PythonDispatchEncounter {event_id=e.id,cat_id=e.cat_id,label=Simulation.Definition(e.cat_id).name,title=r.title,status=v.status,occurred_day=v.occurred_day,resolved_day=v.resolved_day,has_stress=true,return_reward=(float)Simulation.DispatchReceivedReward(e),selected_label=r.choices.SingleOrDefault(c=>c.id==v.choice)?.label,choices=r.choices.Select(c=>new PythonDispatchEncounterOption {choice=c.id,label=c.label,reward_delta=(float)c.reward,fatigue=(float)c.fatigue,stress=(float)c.stress,return_reward=(float)System.Math.Max(0,e.reward+c.reward),reason=v.status=="resolved"?"回答済みです。":Simulation.DispatchChoiceReason(e.id,c.id),can_select=v.status=="waiting" && Simulation.DispatchChoiceReason(e.id,c.id)==""}).ToArray()};
        }).ToArray();
    }
}
