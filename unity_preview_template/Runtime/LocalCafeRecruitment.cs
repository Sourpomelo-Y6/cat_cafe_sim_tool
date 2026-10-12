using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonRecruitment RecruitmentView()
        {
            var s=Simulation.State;string reason=Simulation.RecruitmentRefreshReason();return new PythonRecruitment {housing="在籍 "+s.cats.Count+" / 上限 "+Simulation.HousingCapacity(s,s.day),can_refresh=reason=="",reason=reason,notes="3日ごとに3匹を追加します。以前の候補も残ります。次の紹介："+Simulation.NextRecruitmentDay+"日目（準備中）",traits=new PythonRecruitmentOption[0],coats=new PythonRecruitmentOption[0],hairs=new PythonRecruitmentOption[0],rows=Simulation.RecruitmentCandidates(s).Select(r=>
            {
                var joined=s.recruitment.accepted.SingleOrDefault(p=>p.cat_id==r.cat.id);string problem=Simulation.RecruitReason(r.cat.id);return new PythonRecruitmentCat {cat_id=r.cat.id,content_cat_id=r.cat.id,name=r.cat.name,cost=(float)r.cost,funds_after=(float)(s.funds-r.cost),accepted=joined!=null,accepted_day=joined?.day??0,can_purchase=problem=="",reason=problem,trait_id=r.cat.trait_id,features=r.cat.features,details=r.cat.name+" / "+r.personality+" / "+r.features_label+" / "+r.trait+"\n費用 "+r.cost+" / 受け入れ後の資金 "+(s.funds-r.cost)+"\n"+(joined==null?"健康・体力全回復・休養予定。疲労・ストレス・好感度・経験0で加入します。":joined.day+"日目に受け入れ済み。")};
            }).ToArray()};
        }
    }
}
