using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonObjectiveProgress SidebarProgressView()
        {
            var rows=new[]{"popularity","bond","patron"}.Where(t=>Simulation.AutoTargetStatus(t)!=null && !(t=="popularity" && Simulation.AutoTargetStatus(t)=="cleared")).Select(t=>ObjectiveProgressView(t,false)).ToArray();
            if(rows.Length==0)return null;
            return new PythonObjectiveProgress{label="挑戦中の目標",summary=string.Join("\n",rows.Select(r=>r.label+"："+r.summary)),details=string.Join("\n\n",rows.Select(r=>r.label+"\n"+r.details))};
        }
        private PythonObjectiveProgress ObjectiveProgressView(string mode=null,bool includeOther=true)
        {
            var s=Simulation.State;var rules=Simulation.Rules;mode=mode??s.objective;string label=mode=="popularity"?"人気目標":mode=="patron"?"有力者の満足度":mode=="bond"?"猫6匹との好感度":"自由営業",status=Simulation.AutoTargetStatus(mode)??"active",summary,details;bool continued=mode=="popularity"?s.goal.LastOrDefault()?.continued==true:mode=="patron"?s.patron?.continued==true:mode=="bond" && s.bond_goal?.continued==true;
            string suffix=status=="cleared" && continued?"（達成・継続中）":status!="active" && !continued?"（結果確認待ち）":"";
            if(mode=="popularity" && s.goal.Count>0){var g=s.goal.Last();var r=rules.goal_stages[s.goal.Count-1];int deadline=g.started_day+r.days-1,remaining=System.Math.Max(0,deadline-s.day+(s.phase=="closed"?0:1));summary="第"+s.goal.Count+"段階　人気 "+Number(s.popularity)+" / "+Number(r.target)+"　残り"+remaining+"日";details=summary+"\n期限："+deadline+"日目の閉店まで"+(status=="expired"?"\n期限内未達"+(continued?"・継続営業":""):"");}
            else if(mode=="patron" && s.patron!=null){var p=s.patron;var r=rules.patron;var rows=new[]{r.name+"："+Number(p.satisfaction)+" / "+Number(r.target)}.Concat(r.members.Select(m=>m.name+"："+Number(p.members.Single(v=>v.id==m.id).value)+" / "+Number(m.target))).ToArray();summary=string.Join("　",rows.Select(row=>row.Split('：')[1]));details="全員の満足度を目標まで高めます。\n"+string.Join("\n",rows);}
            else if(mode=="bond" && s.bond_goal!=null){summary="好感度"+Number(rules.bond.affinity)+"以上の在籍猫　"+Simulation.BondQualifying(s).Length+" / "+rules.bond.target+"匹";details=summary+"\n"+string.Join("\n",s.cats.Where(c=>c.activity!="adopted").Select(c=>Simulation.Definition(c.id).name+"："+Number(s.player_cats.FirstOrDefault(p=>p.cat_id==c.id)?.affinity??0)+" / "+Number(rules.bond.affinity)));}
            else{summary="期限なし・自由に営業";details="自由営業にはクリア条件や期限はありません。";}
            var result=new PythonObjectiveProgress{mode=mode,label=label,status=status,summary=summary+suffix,details=details+suffix};
            if(includeOther)foreach(var extra in new[]{"popularity","bond","patron"}.Where(t=>t!=mode && Simulation.AutoTargetStatus(t)!=null)){var other=ObjectiveProgressView(extra,false);result.summary+="\n"+other.label+"："+other.summary;result.details+="\n\n"+other.label+"\n"+other.details;}
            return result;
        }
    }
}
