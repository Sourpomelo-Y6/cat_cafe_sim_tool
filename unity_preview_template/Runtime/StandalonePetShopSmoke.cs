using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public static class StandalonePetShopSmoke
    {
        private static int checks;
        private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Pet shop: "+message);}
        private static CafeResponse Command(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1)
        {
            CafeResponse result=null;var cmd=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=b.Simulation.State.instance_id,expected_revision=b.Simulation.State.revision,kind=kind,choice=choice,cat_id=cat,save_slot=slot,save_id=slot.ToString(),working_cats=new string[0]};
            var it=b.Request("/commands",JsonUtility.ToJson(cmd),r=>result=r);while(it.MoveNext()){}return result;
        }
        private static void Good(LocalCafeBackend b,string kind,string choice=null,string cat=null,int slot=1){var r=Command(b,kind,choice,cat,slot);Check(r.result==UnityWebRequest.Result.Success,kind+": "+r.error);}
        private static CafeSnapshot Clone(LocalCafeBackend b){var s=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(b.Simulation.State));s.NormalizeJsonStrings();CafeDoublePrecision.Restore(s,CafeDoublePrecision.Capture(b.Simulation.State));return s;}
        private static string Capture(LocalCafeBackend b){var s=Clone(b);s.revision=0;s.instance_id="";return JsonUtility.ToJson(s);}
        private static void Reject(LocalCafeBackend b,string kind,string choice=null,string cat=null){string before=Capture(b);Check(Command(b,kind,choice,cat).result==UnityWebRequest.Result.ProtocolError,"Invalid command accepted: "+kind);Check(before==Capture(b),"Rejected command changed state");}
        private static void Save(LocalCafeBackend b,int slot){string before=Capture(b);Good(b,"save_game",slot:slot);Good(b,"load_game",slot:slot);Check(before==Capture(b),"Save/resume changed state");}
        private static void Events(LocalCafeBackend b)
        {
            foreach(var e in b.Simulation.State.dispatches.Where(e=>e.status=="waiting").ToArray())Good(b,"receive_general",e.id,e.cat_id);
            while(b.View().growth_choices.Length>0){var r=b.View().growth_choices[0];Good(b,r.kind,r.choices[0].choice,r.cat_id);}
            if(b.Simulation.IntakeWaiting)Good(b,"resolve_intake","decline");if(b.Simulation.RegularIntroductionWaiting)Good(b,"resolve_regular_introduction","decline");if(b.Simulation.ReservationWaiting)Good(b,"resolve_reservation","decline");
            foreach(var e in b.Simulation.State.trust_events.Where(e=>e.status=="waiting").ToArray())throw new Exception("Unexpected trust event in isolated fixture");
        }
        public static void Verify(){string root=Environment.GetEnvironmentVariable("CATCAFE_PET_SHOP_SMOKE")??"pet-shop-smoke";Directory.CreateDirectory(root);int total=Run(root);File.WriteAllText(Path.Combine(root,"result.json"),"{\"passed\":true,\"checks\":"+total+"}");}
        public static int Run(string root)
        {
            checks=0;
            foreach(string mode in new[]{"free","popularity","bond","patron"})
            {
                string path=Path.Combine(root,mode);var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
                rules.starting_funds=20000;rules.store_events.probability=0;rules.rules_id+="-pet-shop";rules.goal_stages[0].target=101;rules.goal_stages[0].days=30;
                rules.growth.threshold=1;rules.max_probability=rules.stress_per_service_tick=rules.fatigue_per_service_tick=0;
                var b=new LocalCafeBackend(path,rules);
                if(File.Exists(Path.Combine(path,"slot-8.json")))foreach(int slot in Enumerable.Range(2,7))
                {
                    Good(b,"load_game",slot:slot);Check(b.Simulation.State.objective==mode,"Restart changed original mode");
                    Check(b.Simulation.State.shop_purchases.Count==(slot==2?0:slot==3?1:3),"Restart lost purchases");
                    Check(b.View().pet_shop.rows.Count(r=>r.accepted)==b.Simulation.State.shop_purchases.Count,"Restart changed catalog ownership");
                    if(slot==5)Check(b.Simulation.PlayerActive && b.Simulation.ActivePlayer.cat_id=="shop-3","Restart lost purchased cat's active exchange");
                    if(slot==7)Check(b.Simulation.State.dispatches.Any(e=>e.cat_id=="shop-1" && e.status=="travelling"),"Restart lost active dispatch");
                }
                Good(b,"new_game",mode);var rows=b.View().pet_shop.rows;Check(rows.Length==3 && rows[0].name=="チロ" && rows[1].name=="レオ" && rows[2].name=="ナナ" && rows.Select(r=>r.cost).SequenceEqual(new[]{400f,450f,500f}),"Fixed source catalog differs");Save(b,2);
                foreach(var rule in rules.pet_shop)
                {
                    double funds=b.Simulation.State.funds;
                    foreach(double amount in new[]{rule.cost-1,rule.cost,rule.cost+1}){b.Simulation.State.funds=amount;Check((b.Simulation.CatPurchaseReason(rule.cat.id)=="")==(amount>rule.cost),"Purchase funds boundary differs");Check(b.View().pet_shop.rows.Single(r=>r.cat_id==rule.cat.id).can_purchase==(amount>rule.cost),"UI admission flag differs");}
                    b.Simulation.State.funds=funds;
                }
                Reject(b,"purchase_cat",cat:"unknown");Reject(b,"purchase_cat",cat:"cat-mugi");Good(b,"player_begin",cat:"cat-mugi");Reject(b,"purchase_cat",cat:"shop-1");Good(b,"player_finish");
                Good(b,"purchase_cat",cat:"shop-1");Check(b.Simulation.State.funds==19600 && b.Simulation.State.pet_shop_expenses==400 && b.Simulation.State.recruitment_expenses==0 && b.View().finance.pet_shop_expenses==400,"Purchase finance categories differ");Save(b,3);
                Reject(b,"purchase_cat",cat:"shop-1");Reject(b,"purchase_cat",cat:"shop-2");Check(b.View().pet_shop.rows.Single(r=>r.cat_id=="shop-2").reason.Contains("満員"),"Full housing reason missing");
                Good(b,"purchase_housing");Good(b,"purchase_cat",cat:"shop-2");Good(b,"purchase_cat",cat:"shop-3");Check(b.Simulation.State.pet_shop_expenses==1350 && b.Simulation.State.shop_purchases.Count==3,"All purchases charged incorrect expense");Save(b,4);
                foreach(var rule in rules.pet_shop)
                {
                    var cat=b.Simulation.State.cats.Single(c=>c.id==rule.cat.id);Check(!cat.working && cat.health=="healthy" && cat.stamina==rules.max_stamina && cat.fatigue==0 && cat.stress==0 && b.Simulation.PlayerCat(cat.id).affinity==0,"Joined shop cat initialization differs");
                    Check(b.View().pet_shop.rows.Single(r=>r.cat_id==cat.id).accepted_day==1,"Purchase day lost");Reject(b,"purchase_cat",cat:cat.id);
                }
                for(int variant=0;variant<8;variant++)
                {
                    string before=Capture(b);var bad=Clone(b);
                    if(variant==0)bad.shop_purchases[0].day=0;else if(variant==1)bad.shop_purchases[0].day=bad.day+1;else if(variant==2)bad.shop_purchases.Add(bad.shop_purchases[0]);else if(variant==3)bad.shop_purchases[0].cat_id="unknown";else if(variant==4)bad.pet_shop_expenses++;else if(variant==5)bad.cats.RemoveAll(c=>c.id=="shop-1");else if(variant==6)bad.shop_purchases=null;else bad.shop_purchases.Clear();
                    bool rejected=false;try{b.Simulation.Restore(bad);}catch(ArgumentException){rejected=true;}Check(rejected && before==Capture(b),"Corrupt shop save accepted or changed state");
                }
                Good(b,"player_begin",cat:"shop-3");Good(b,"player_step","direct");Save(b,5);Good(b,"player_finish");
                b.Simulation.Execute("set_shifts",working:new[]{"shop-1","shop-2","shop-3"});Good(b,"start_business");Reject(b,"purchase_cat",cat:"shop-1");
                while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Save(b,6);Events(b);if(b.Simulation.GoalWaiting)Good(b,"advance_goal");Reject(b,"purchase_cat",cat:"shop-2");Good(b,"next_day");Events(b);
                // Automatic assignment may serve only two of the three cats. Give each untrained cat a full shift.
                foreach(var rule in rules.pet_shop.Where(r=>string.IsNullOrEmpty(b.Simulation.State.growth.Single(g=>g.cat_id==r.cat.id).specialization)).ToArray())
                {
                    b.Simulation.Execute("set_shifts",working:new[]{rule.cat.id});Good(b,"start_business");while(b.Simulation.State.phase!="closed"){Events(b);Good(b,"advance_business");}Events(b);
                    if(b.Simulation.GoalWaiting)Good(b,b.Simulation.State.goal.Count<rules.goal_stages.Length?"advance_goal":"continue_goal");Good(b,"next_day");Events(b);
                }
                Check(rules.pet_shop.All(r=>!string.IsNullOrEmpty(b.Simulation.State.growth.Single(g=>g.cat_id==r.cat.id).specialization)),"Purchased cats did not grow through service");
                Good(b,"dispatch_general","neighborhood_visit","shop-1");Save(b,7);Good(b,"day_off");Events(b);Check(b.Simulation.State.dispatches.Any(e=>e.cat_id=="shop-1" && e.status=="resolved"),"Purchased cat dispatch failed");
                Check(b.Simulation.State.history[0].pet_shop_expenses==1350 && b.Simulation.State.pet_shop_expenses==0,"Daily purchase finance reset differs");Save(b,8);
            }
            return checks;
        }
    }
}
