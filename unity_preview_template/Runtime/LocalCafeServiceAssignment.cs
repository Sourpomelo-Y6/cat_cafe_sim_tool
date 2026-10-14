using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonServiceAssignment ServiceAssignmentView()
        {
            var s=Simulation.State;var free=Simulation.AssignmentFreeSeats;
            return new PythonServiceAssignment {auto_assign=s.auto_assignment,waiting=Simulation.AssignmentWaiting,can_assign=Simulation.AssignmentReason()=="",reason=Simulation.AssignmentReason(),
                notes="手動の担当待ちでは営業時計を止めます。割り当てだけでは時刻・資金・体力は進みません。接客は営業を進める操作で進行します。自動設定をONにしても接客中の担当は維持します。",
                customers=s.queue.Select(id=>{var v=s.visits.Single(r=>r.id==id);return new PythonAssignmentCustomer {customer_id=id,name=Details.CustomerName(id),details="待機 "+(s.tick-v.arrival)+"tick / 猶予の残り "+System.Math.Max(0,Simulation.MaxWaitTicks-(s.tick-v.arrival))+"tick",matches=s.cats.Select(c=>new PythonAssignmentMatch {cat_id=c.id,details="好み："+Simulation.AssignmentPreference(id)+" / 相性："+(Simulation.AssignmentMatched(c.id,id)?"一致":"不一致")+" / 親しみ："+(s.relationships.Find(r=>r.cat_id==c.id && r.customer_id==id)?.affinity??0)}).ToArray()};}).ToArray(),
                cats=s.cats.Select(c=>new PythonAssignmentCat {cat_id=c.id,name=Simulation.Definition(c.id).name,available=Simulation.AssignmentCatReason(c.id)=="",reason=Simulation.AssignmentCatReason(c.id),details=c.activity+" / "+(c.working?"出勤":"休養")+" / "+c.health+" / 体力 "+c.stamina+" / "+Simulation.MaxStamina(c.id)+"・疲労 "+c.fatigue}).ToArray(),
                seats=Enumerable.Range(0,s.seat_count).Select(i=>{var e=s.exchanges.Find(r=>r.seat==i);bool available=free.Contains(i);return new PythonAssignmentSeat {seat_id="seat-"+(i+1),label="席 "+(i+1),available=available,reason=available?"":"接客中または使用できない席です。",details=(e==null?available?"空席":"使用不可":"接客中："+Details.CustomerName(e.customer_id)+" / "+Simulation.Definition(e.cat_id).name)+" / "+(Simulation.Installed(i)?.name??"設備なし")};}).ToArray()};
        }
    }
}
