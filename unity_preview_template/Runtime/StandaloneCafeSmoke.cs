using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    public static class StandaloneCafeSmoke
    {
        private static int checks;
        private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
        private static CafeResponse Request(LocalCafeBackend b, string route, string body = null)
        {
            CafeResponse response = null; var request = b.Request(route, body, r => response = r);
            while (request.MoveNext()) { }
            return response;
        }
        private static PythonShiftCommand Command(LocalCafeBackend b, string kind, int slot = 1) => new PythonShiftCommand
        {
            request_id = Guid.NewGuid().ToString("N"), instance_id = b.Simulation.State.instance_id, expected_revision = b.Simulation.State.revision,
            kind = kind, choice = "free", working_cats = new string[0], save_slot = slot, save_id = slot.ToString()
        };
        private static void Send(LocalCafeBackend b, string kind, int slot = 1)
        {
            var reply = Request(b, "/commands", JsonUtility.ToJson(Command(b, kind, slot)));
            Check(reply.result == UnityWebRequest.Result.Success, kind + ": " + reply.error);
        }
        public static void Run(string reportPath)
        {
            checks = 0;
            string directory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "player-smoke-saves");
            var b = new LocalCafeBackend(directory);
            bool restarted = File.Exists(Path.Combine(directory, "slot-3.json"));
            if (restarted) { Send(b, "load_game", 3); Check(b.Simulation.State.day == 2 && b.Simulation.State.phase == "preparation", "Cross-process restart"); }
            Send(b, "new_game");
            var command = Command(b, "start_business"); string payload = JsonUtility.ToJson(command);
            var first = Request(b, "/commands", payload); Check(first.result == UnityWebRequest.Result.Success, "Start failed");
            int tick = b.Simulation.State.tick; double funds = b.Simulation.State.funds;
            Check(Request(b, "/commands", payload).text == first.text && b.Simulation.State.tick == tick && b.Simulation.State.funds == funds, "Duplicate command changed state");
            command.kind = "advance_business";
            Check(Request(b, "/commands", JsonUtility.ToJson(command)).result == UnityWebRequest.Result.ProtocolError && b.Simulation.State.tick == tick, "Reused ID accepted");
            var stale = Command(b, "advance_business"); stale.expected_revision--;
            Check(Request(b, "/commands", JsonUtility.ToJson(stale)).result == UnityWebRequest.Result.ProtocolError && b.Simulation.State.tick == tick, "Stale command accepted");
            while (b.Simulation.State.tick < 11) Send(b, "advance_business");
            Check(b.Simulation.State.exchanges.Count > 0, "No active service at checkpoint"); Send(b, "save_game", 2);
            while (b.Simulation.State.phase != "closed") Send(b, "advance_business");
            funds = b.Simulation.State.funds; double revenue = b.Simulation.State.revenue; int completed = b.Simulation.State.completed;
            double popularity = b.Simulation.State.popularity, popularityGain = b.Simulation.State.popularity_gain;
            var projected = JsonUtility.FromJson<PythonCafeState>(Request(b, "/state").text);
            Check(projected.popularity == popularity && projected.popularity_gain == popularityGain, "Popularity projection mismatch");
            Check(projected.customer_satisfaction.satisfied + projected.customer_satisfaction.normal + projected.customer_satisfaction.dissatisfied == completed, "Satisfaction projection mismatch");
            Send(b, "load_game", 2);
            while (b.Simulation.State.phase != "closed") Send(b, "advance_business");
            Check(b.Simulation.State.funds == funds && b.Simulation.State.revenue == revenue && b.Simulation.State.completed == completed, "Resume changed settlement");
            Send(b, "save_game", 2); Send(b, "load_game", 2);
            Check(b.Simulation.State.funds == funds, "Closed save charged expenses again");
            Check(b.Simulation.State.popularity == popularity && b.Simulation.State.popularity_gain == popularityGain, "Resume changed popularity settlement");
            Send(b, "next_day"); Send(b, "save_game", 3);
            Check(Request(b, "/python-save?path=unused").result == UnityWebRequest.Result.ProtocolError, "Unsupported legacy route accepted");
            var bad = Command(b, "load_game"); bad.save_id = "../anything";
            Check(Request(b, "/commands", JsonUtility.ToJson(bad)).result == UnityWebRequest.Result.ProtocolError, "Path used as save ID");
            checks += StandaloneStoreEventSmoke.Run(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "store-event-smoke-saves"));
            checks += StandaloneCustomerSmoke.Run(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "customer-smoke-saves"));
            checks += StandaloneIntakeSmoke.Run(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "intake-smoke-saves"));
            checks += StandaloneExpansionSmoke.Run(Path.Combine(directory,"expansion-smoke-saves"));
            checks += StandaloneExpansionSmoke.Run(Path.Combine(directory,"specialist-decline-smoke-saves"),"decline");
            checks += StandaloneExpansionSmoke.Run(Path.Combine(directory,"specialist-failure-smoke-saves"),requiredOpen:999);
            checks += StandaloneExpansionSmoke.Run(Path.Combine(directory,"specialist-unserved-smoke-saves"),unserved:true);
            foreach(string kind in new[]{"quiet_space","cat_tower","grooming_brush"})checks+=StandaloneServiceSmoke.Run(Path.Combine(directory,"service-"+kind+"-smoke-saves"),kind);
            checks+=StandaloneServiceSmoke.Run(Path.Combine(directory,"service-failure-smoke-saves"),"cat_tower",true);
            foreach(string customer in new[]{"advanced-longhair","vip-calico"})foreach(bool fail in new[]{false,true})checks+=StandaloneLateCustomerSmoke.Run(Path.Combine(directory,"late-"+customer+"-"+fail),customer,fail);
            foreach(string variant in new[]{"cleared","expired","funds","popularity"})checks+=StandaloneClearResultsSmoke.Run(Path.Combine(directory,"clear-"+variant),variant);
            foreach(string destination in new[]{"out_of_town_visit","quiet_reading_salon","cat_exercise_class","cat_product_trial","small_art_museum"})foreach(bool second in new[]{false,true})checks+=StandaloneDispatchDestinationsSmoke.Run(Path.Combine(directory,"destination-"+destination+"-"+second),destination,second);
            foreach(string destination in new[]{"shopping_street_event","cat_photo_studio"})foreach(bool accept in new[]{false,true})foreach(bool closed in new[]{false,true})checks+=StandaloneDispatchIntroductionSmoke.Run(Path.Combine(directory,"introduction-"+destination+"-"+accept+"-"+closed),destination,accept,closed);
            checks += StandaloneDispatchSmoke.Run(Path.Combine(directory,"dispatch-smoke-saves"));
            checks += StandaloneItemsSmoke.Run(Path.Combine(directory,"items-smoke-saves"));
            checks += StandaloneHousingSmoke.Run(Path.Combine(directory, "housing-smoke-saves"));
            checks += StandaloneGrowthSmoke.Run(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "growth-smoke-saves"));
            checks += StandaloneCustomerTrustSmoke.Run(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "customer-trust-smoke-saves"));
            checks += StandaloneRegularIntroductionSmoke.Run(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "regular-introduction-smoke-saves"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            File.WriteAllText(reportPath, "{\"passed\":true,\"checks\":" + checks + ",\"cross_process_restart\":" + restarted.ToString().ToLowerInvariant() + ",\"day\":" + b.Simulation.State.day + ",\"funds\":" + funds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
        }
    }
}
