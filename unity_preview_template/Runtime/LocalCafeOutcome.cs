using CatCafePreview.Standalone;
namespace CatCafePreview {public sealed partial class LocalCafeBackend {
 CafeOutcome outcome;string outcomeEpoch;
 public CafeOutcome Outcome {get{var s=Simulation.State;if(outcomeEpoch!=s.instance_id){outcome=null;outcomeEpoch=s.instance_id;}string cause=CafeOutcome.Cause(s);if(cause!=null && (outcome==null || outcome.cause!=cause))outcome=CafeOutcome.Capture(s,Simulation.Rules);return outcome;}}
 public bool OutcomePending=>CafeOutcome.Cause(Simulation.State)==Outcome?.cause && Outcome!=null && !Outcome.progress.completed;
 void RestoreOutcome(CafeOutcome row){outcome=row??CafeOutcome.Capture(Simulation.State,Simulation.Rules,true);outcomeEpoch=Simulation.State.instance_id;}
}}
