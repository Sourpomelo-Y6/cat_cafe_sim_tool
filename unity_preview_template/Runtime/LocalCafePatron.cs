using System;
using System.Linq;
namespace CatCafePreview
{
    [Serializable] public class PythonPatronGoal {public string status,details,reason;public bool pending,can_continue;}
    public sealed partial class LocalCafeBackend
    {
        private static string PatronFeatureLabel(string feature)=>feature=="white"?"白猫":feature=="black"?"黒猫":feature=="long_hair"?"長毛":feature??"";
        private PythonPatronGoal PatronGoalView()
        {
            var p=Simulation.State.patron;if(p==null)return null;var r=Simulation.Rules.patron;
            return new PythonPatronGoal {status=p.status,pending=Simulation.PatronWaiting,can_continue=Simulation.PatronContinueReason()=="",reason=Simulation.PatronContinueReason(),details=r.name+"："+Number(p.satisfaction)+" / "+Number(r.target)+"\n"+string.Join("\n",r.members.Select(m=>m.name+"："+Number(p.members.Single(s=>s.id==m.id).value)+" / "+Number(m.target)))+"\n"+(p.continued?"クリア・継続中":p.status=="cleared"?"クリア・結果確認待ち":"挑戦中（期限なし）")+PatronGoalGuide()};
        }
        public string PatronFixedTerms(CatCafePreview.Standalone.CafeDispatch e)
        {
            var m=e.patron_match;
            string condition=m==null?"歓迎条件："+PatronFeatureLabel(Simulation.Rules.patron.destination.welcome.feature)+" "+(e.feature_matched?"一致":"不一致")+" / "+GrowthLabel(Simulation.Rules.patron.destination.welcome.specialization)+"専門 "+(e.specialization_matched?"一致":"不一致"):
                "出発時の希望："+PatronConditionLabel(m.condition)+" / "+(m.matched?"一致":"不一致");
            return condition+" / 固定報酬 "+Number(Simulation.DispatchReceivedReward(e))+" / 固定加点 ＋"+Number(Simulation.PatronEventGain(e));
        }
        private PythonPatronDispatch PatronDispatchView()
        {
            var s=Simulation.State;var options=s.patron==null?new PythonDispatchOption[0]:s.cats.SelectMany(c=>Simulation.PatronDestinations().Select(r=>
            {
                string conditions=PatronCurrentGuide(r.id)+"\n出発可否："+(Simulation.DispatchReason(c.id,r.id)==""?"出発可能（希望一致とは別）":Simulation.DispatchReason(c.id,r.id))+"\n今回の見込み："+PatronFixedTerms(Simulation.PatronPreview(c.id,r.id)).Replace("出発時の希望：","現在の希望：");
                return new PythonDispatchOption {cat_id=c.id,choice=r.id,label=Simulation.Definition(c.id).name+" / "+r.name,days=r.days,reward=(float)Simulation.DispatchReward(c.id,r.id),satisfaction=(float)Simulation.PatronGain(c.id,r.id),detail=conditions+" / 満足度＋"+Number(Simulation.PatronGain(c.id,r.id))+"（出発時固定） / 疲労"+Number(r.max_fatigue)+"以下 / 出発費用なし",reason=Simulation.DispatchReason(c.id,r.id),can_select=Simulation.DispatchReason(c.id,r.id)==""};
            })).ToArray();
            return new PythonPatronDispatch {options=options,events=s.dispatches.Where(e=>Simulation.IsPatronDestination(e.destination)).Select(e=>new PythonDispatchEvent {cat_id=e.cat_id,choice=e.id,label=Simulation.Definition(e.cat_id).name+" / "+Simulation.DispatchDestination(e.destination).name,status=e.status,remaining=e.remaining,reward=(float)Simulation.DispatchReceivedReward(e),satisfaction=(float)Simulation.PatronEventGain(e),needs_response=e.status=="waiting",can_select=e.status=="waiting" && Simulation.DispatchReceiveReason(e.id)=="",reason=Simulation.DispatchReceiveReason(e.id),detail=PatronFixedTerms(e)+" / 残り "+e.remaining+"回の閉店 / 満足度加点は帰還確認時に目標上限まで反映"}).ToArray()};
        }
    }
}
