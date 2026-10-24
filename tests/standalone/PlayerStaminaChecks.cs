using System;
using System.IO;
using System.Linq;
using CatCafePreview.Standalone;
partial class Program
{
 static void PlayerStaminaChecks(string rulesPath)
 {
  var rules=Decode<CafeRules>(File.ReadAllText(rulesPath));var sim=new CafeSimulation(rules);int checks=0;
  void Check(bool ok,string msg){checks++;Assert(ok,msg);}
  var c=sim.State.cats.First();
  foreach(bool working in new[]{false,true})foreach(double value in new[]{0d,1d,rules.exchange.low_stamina,rules.exchange.low_stamina+.01,100d})
  {
   c.working=working;c.stamina=value;string before=Encode(sim.State),guide=sim.PlayerStaminaGuidance(c.id);
   Check(guide.Contains("低体力")== (working && value>0 && value<=rules.exchange.low_stamina),"low threshold");
   Check(guide.Contains("体力切れ")== (working && value==0),"exhaustion warning");Check(before==Encode(sim.State),"guide mutation");
  }
  c.stamina=100;sim.Execute("player_begin",catId:c.id);sim.Execute("player_step",choice:"direct");
  Check(sim.PlayerStaminaGuidance(c.id).StartsWith("出勤予定"),"active plan");
  sim.Execute("player_finish");double remaining=c.stamina;var other=sim.State.cats[1];other.working=true;
  sim.Execute("set_shifts",working:sim.State.cats.Where(x=>x.id!=c.id && x.working).Select(x=>x.id).ToArray());
  Check(c.stamina==remaining && other.working,"rest instantly recovered or changed other cat");
  Check(sim.PlayerStaminaGuidance(c.id)=="休養予定","rest guide");
  sim.Restore(Decode<CafeSnapshot>(Encode(sim.State)));Check(sim.PlayerStaminaGuidance(c.id)=="休養予定","resume guide");
  // Growth data is a boundary fixture, with matching saved experience and stage.
  c=sim.State.cats.Single(x=>x.id==c.id);c.stamina_stage=20;sim.GrowthCat(c.id).rest=c.stamina_experience_origin+400;c.stamina=299;
  Check(sim.MaxStamina(c.id)==300,"grown maximum fixture");
  Check(sim.PlayerStaminaGuidance(c.id)=="休養予定","grown guide");
  // Exchange warning uses the saved rules threshold, independent of max stamina.
  c=sim.State.cats.Single(x=>x.id==c.id);c.working=true;c.stamina=rules.exchange.low_stamina+.01;
  Check(!sim.PlayerStaminaGuidance(c.id).Contains("低体力"),"relative threshold introduced");
  Console.WriteLine("PASS player stamina guidance: "+checks);
 }
}
