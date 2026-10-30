#if CATCAFE_DEV_ACQUISITION
using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed class DevCollectionRoute
    {
        public string id,objective;
        public string[] targets=new string[0];
    }
    public sealed partial class CafeDevAcquisition
    {
        static DevCollectionRoute[] CreateRoutes(CafeRules rules,DevAcquisitionPlan plan)
        {
            var list=plan.objectives.Select(o=>new DevCollectionRoute{id=o,objective=o}).ToList();
            if(!plan.collection_routes)return list.ToArray();
            var main=new List<string>{"cat/"+rules.intake.cat.id,"cat/"+rules.regular_introduction.cat.id,"cat/"+rules.visiting_cat.cat.id};
            main.AddRange(rules.pet_shop.Select(p=>"cat/"+p.cat.id));main.AddRange(rules.equipment.Select(e=>"equipment/"+e.id));
            main.AddRange(new[]{"rest_space","rest_space_upgrade","rest_space_soundproof","waiting_area","waiting_area_upgrade","housing","housing_upgrade"}.Select(id=>"facility/"+id));
            main.AddRange(Enumerable.Range(1,rules.expansion_costs.Length).Select(i=>"facility/seat-expansion-"+i));
            list.Add(new DevCollectionRoute{id="collect-main",objective="popularity",targets=main.ToArray()});
            var dispatch=rules.dispatch_introductions.Select(d=>"cat/intro-"+d.destination).Concat(rules.dispatch_destinations.Where(d=>string.IsNullOrEmpty(d.unsupported_reason)).Select(d=>"dispatch/"+d.id)).Concat(rules.item_sale_prices.Select(i=>"supply/"+i.id)).ToList();
            foreach(var trait in rules.dispatch_destinations.Select(d=>d.required_trait).Where(t=>!string.IsNullOrEmpty(t)).Distinct().Where(t=>!rules.cats.Any(c=>c.trait_id==t)))
            {
                var candidate=rules.recruitment_catalog.FirstOrDefault(c=>c.cat.trait_id==trait);
                if(candidate==null)throw new ArgumentException("No normal admission source for dispatch trait: "+trait);
                dispatch.Add("cat/"+candidate.cat.id);
            }
            list.Add(new DevCollectionRoute{id="collect-dispatch",objective="popularity",targets=dispatch.ToArray()});
            int slots=rules.housing.initial_capacity+rules.housing.capacity_bonus+rules.housing_upgrade.capacity_bonus-rules.cats.Length;
            int batch=Math.Min(6,slots);if(batch<1)throw new ArgumentException("No collection admission capacity");
            for(int offset=0;offset<rules.recruitment_catalog.Length;offset+=batch)
                list.Add(new DevCollectionRoute{id="collect-catalog-"+(offset/batch+1),objective="free",targets=rules.recruitment_catalog.Skip(offset).Take(batch).Select(c=>"cat/"+c.cat.id).ToArray()});
            if(list.Count>12)throw new ArgumentException("Collection plan exceeds 12-route budget");
            return list.ToArray();
        }
        bool WantCat(string id)=>!Collecting || routes[route].targets.Contains("cat/"+id);
        string CollectionImpossible()
        {
            if(!Collecting)return null;var s=Simulation.State;
            if(WantCat(rules.intake.cat.id) && s.intake_request.status=="declined")return "one_time_admission_declined/"+rules.intake.cat.id;
            if(WantCat(rules.regular_introduction.cat.id) && s.regular_introduction.status=="declined")return "one_time_admission_declined/"+rules.regular_introduction.cat.id;
            if(WantCat("intro-cat_photo_studio") && s.dispatches.Any(d=>d.destination=="cat_photo_studio" && d.introduction?.status=="declined"))return "one_time_admission_declined/intro-cat_photo_studio";
            return null;
        }
        bool AssignedComplete()
        {
            var got=new HashSet<string>(Targets().Where(t=>t.status=="acquired").Select(t=>t.category+"/"+t.id));
            return routes[route].targets.All(got.Contains);
        }
        CafeAutoOperation PlanCollection()
        {
            var sim=Simulation;var s=sim.State;var assigned=new HashSet<string>(routes[route].targets);
            var got=new HashSet<string>(Targets().Where(t=>t.status=="acquired").Select(t=>t.category+"/"+t.id));
            double reserve=Math.Max(600,3*sim.OperatingCostEstimate);
            bool Need(string kind,string id)=>assigned.Contains(kind+"/"+id) && !got.Contains(kind+"/"+id);
            // Expand before filling slots; each route reserves room only for its own admissions.
            bool admits=assigned.Any(t=>t.StartsWith("cat/") && !got.Contains(t));
            int reserved=assigned.Count(t=>t.StartsWith("cat/") && !got.Contains(t) &&
                (t=="cat/"+rules.intake.cat.id || t=="cat/"+rules.regular_introduction.cat.id || t=="cat/"+rules.visiting_cat.cat.id || t.StartsWith("cat/intro-",StringComparison.Ordinal)));
            bool spareAdmission=s.cats.Count(c=>c.activity!="adopted")+reserved<sim.HousingCapacity(s,s.day);
            if((admits || Need("facility","housing")) && sim.HousingReason("purchase_housing")=="" && s.funds-rules.housing.cost>=reserve)return Op("purchase_housing");
            if((admits || Need("facility","housing_upgrade")) && sim.HousingReason("upgrade_housing")=="" && s.funds-rules.housing_upgrade.cost>=reserve)return Op("upgrade_housing");
            foreach(var p in rules.pet_shop)if(spareAdmission && Need("cat",p.cat.id) && sim.CatPurchaseReason(p.cat.id)=="" && s.funds-p.cost>=reserve)return Op("purchase_cat",p.cat.id);
            bool recruits=rules.recruitment_catalog.Any(c=>Need("cat",c.cat.id));
            if(recruits)
            {
                if(sim.RecruitmentRefreshReason()=="")return Op("open_recruitment");
                foreach(var c in sim.RecruitmentCandidates(s))
                    if(spareAdmission && Need("cat",sim.ContentImageId(c.cat.id)) && sim.RecruitReason(c.cat.id)=="" && s.funds-c.cost>=reserve)return Op("recruit_cat",c.cat.id);
            }
            foreach(var item in rules.item_shop)if(Need("supply",item.item.id) && sim.ItemPurchaseReason(item.item.id)=="" && s.funds-item.cost>=reserve)return Op("purchase_item",choice:item.item.id);
            foreach(var e in rules.equipment)if(Need("equipment",e.id) && sim.EquipmentPurchaseReason(e.id)=="" && s.funds-e.cost>=reserve)return Op("purchase_seat_equipment",choice:e.id,seat:"seat-1");
            var facility=new[]{"purchase_rest_space","upgrade_rest_space","soundproof_rest_space","purchase_waiting_area","upgrade_waiting_area"};
            var ids=new[]{"rest_space","rest_space_upgrade","rest_space_soundproof","waiting_area","waiting_area_upgrade"};
            var costs=new[]{rules.rest_space.cost,rules.rest_space_upgrade.cost,rules.rest_space_soundproof.cost,rules.waiting_area.cost,rules.waiting_area_upgrade.cost};
            for(int i=0;i<facility.Length;i++)if(Need("facility",ids[i]) && (i<3?sim.RestSpaceReason(facility[i]):sim.WaitingAreaReason(facility[i]))=="" && s.funds-costs[i]>=reserve)return Op(facility[i]);
            // Avoid same-day post-clear expansion: normal persistence requires the following day.
            if(Need("facility","seat-expansion-"+(s.expansion.Count+1)) && sim.ExpansionReason()=="" && s.goal.All(g=>g.status!="cleared" || g.resolved_day<s.day) && s.funds-rules.expansion_costs[s.expansion.Count]>=reserve)return Op("expand_seats");
            foreach(var d in rules.dispatch_destinations)
            {
                bool introduction=Need("cat","intro-"+d.id);
                if(!Need("dispatch",d.id) && !introduction)continue;
                if(s.dispatches.Any(e=>e.destination==d.id && (e.status!="resolved" || introduction && e.introduction!=null && e.introduction.status!="declined")))continue;
                var cat=s.cats.OrderBy(c=>c.fatigue+c.stress).ThenBy(c=>c.id,StringComparer.Ordinal).FirstOrDefault(c=>sim.DispatchReason(c.id,d.id)=="");
                if(cat!=null)return Op("dispatch_general",cat.id,d.id);
            }
            return null;
        }
    }
}
#endif
