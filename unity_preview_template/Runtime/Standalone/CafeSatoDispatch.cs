using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeSatoDispatch
    {
        public string cat_id;
        public int requested_day, accepted_day, declined_day, returned_day;
        public bool refused_return, returned_after_service, epilogue_read;
        public CafeConversationProgress conversation;
    }

    [Serializable] public sealed class CafeSatoAffinityScene
    {
        public string cat_id;
        public CafeConversationProgress conversation;
    }

    public sealed partial class CafeSimulation
    {
        public const string Sato = "guest-1";
        public const string SatoDestination = "sato_request";
        private const double SatoAffinity = 80;
        private const double SatoDiscontent = 50;
        internal static readonly DispatchRules SatoVisit = new DispatchRules
            { id=SatoDestination, name="佐藤さんへの指名派遣", days=1, max_fatigue=60 };

        private static CafeConversationProgress SatoScene(string id, string branch)
            => new CafeConversationProgress { conversation_id=id, branch_id=branch, viewed_branches=Array.Empty<string>() };

        private void ObserveSatoAffinity(string catId, string customer, double affinity)
        {
            if (State.import_history != null || customer != Sato || affinity < SatoAffinity || State.sato_affinity.Any(e => e.cat_id == catId)) return;
            State.sato_affinity.Add(new CafeSatoAffinityScene { cat_id=catId, conversation=SatoScene("sato.affinity.v1","affinity") });
        }

        private void PresentSatoDispatch()
        {
            if (State.import_history != null) return;
            var request = State.sato_dispatch;
            if (request != null)
            {
                if (State.day == request.requested_day + 1 && request.accepted_day == 0 && request.declined_day == 0)
                    request.conversation = SatoScene("sato.request.v1","introduction");
                if (request.accepted_day != 0 || request.declined_day != 0 || State.day < request.requested_day + 2
                    || SatoMissing(State) != null) return;
                var cat = State.cats.Single(c => c.id == request.cat_id);
                if (cat.activity == "adopted") request.declined_day = State.day;
                else if (cat.activity == "cafe") StartSatoMissing(cat, "sato_expired");
                return;
            }

            if (DiscontentScore(Sato, State.day) < SatoDiscontent
                || State.trust_customers.Any(c => c.customer_id == Sato && c.status != "stable")
                || State.adoption?.events.Any(e => e.customer_id == Sato && e.status == "waiting") == true) return;
            var candidate = State.relationships
                .Where(r => r.customer_id == Sato && r.affinity >= SatoAffinity && State.sato_affinity.Any(e => e.cat_id == r.cat_id))
                .Where(r => State.cats.Any(c => c.id == r.cat_id && c.activity == "cafe" && c.health == "healthy"
                    && !c.cannot_continue && c.stamina > 0 && c.fatigue <= SatoVisit.max_fatigue))
                .OrderByDescending(r => r.affinity).ThenBy(r => r.cat_id, StringComparer.Ordinal).FirstOrDefault();
            if (candidate == null) return;
            if (State.cats.Count(c => c.activity == "cafe" && c.health == "healthy" && !c.cannot_continue && c.stamina > 0) <= State.seat_count) return;
            State.sato_dispatch = new CafeSatoDispatch { cat_id=candidate.cat_id, requested_day=State.day,
                conversation=SatoScene("sato.request.v1","introduction") };
        }

        public const string SatoAdoptionSource = "sato-return";

        private void PresentSatoAdoption()
        {
            var missing = SatoMissing(State);
            if (missing == null || missing.resolved_day == 0 || missing.resolved_day + 1 != State.day
                || State.adoption?.enabled != true) return;
            var cat = State.cats.Single(c => c.id == missing.cat_id);
            if (cat.activity != "cafe") return;
            double affinity = State.relationships.Find(r => r.cat_id == cat.id && r.customer_id == Sato)?.affinity ?? 0;
            double player = PlayerCat(cat.id).affinity;
            if (affinity < Math.Max(80,Rules.adoption_threshold) || affinity <= player || player > 40) return;
            State.adoption.events.Add(new CafeAdoptionEvent { id="adoption-"+State.day+"-"+cat.id,
                cat_id=cat.id, customer_id=Sato, source=SatoAdoptionSource, day=State.day, tick=0,
                guest_affinity=affinity, player_affinity=player, threshold=Rules.adoption_threshold });
        }

        public bool SatoEpilogueAvailable(string catId) => SatoEpilogueAvailable(State,catId);

        private static bool SatoEpilogueAvailable(CafeSnapshot s,string catId)
            => s.adoption?.events.Any(e => e.source == SatoAdoptionSource && e.cat_id == catId
                && e.status == "resolved" && e.choice == "accept" && s.day - e.resolved_day >= 3) == true;

        private static CafeMissingCase SatoMissing(CafeSnapshot state)
            => state.missing_cases?.FirstOrDefault(e => e.reason == "sato_expired" || e.reason == "sato_refused");

        private static int SatoTrustDay(CafeSnapshot state, string customer, int day)
        {
            var e = state.sato_dispatch;
            if (customer != Sato || e == null || day < e.requested_day || day == int.MaxValue) return day;
            int end = Math.Max(e.returned_day, e.declined_day);
            return end == 0 ? int.MaxValue : Math.Max(day, end + 1);
        }

        private bool SatoDispatchAwaitingAnswer => State.sato_dispatch is CafeSatoDispatch e && e.accepted_day == 0
            && e.declined_day == 0 && State.day < e.requested_day + 2;

        public string SatoDispatchResponseReason(bool accept)
        {
            if (!SatoDispatchAwaitingAnswer) return "この依頼の回答期限は終了しました。";
            if (State.game_over != null || State.phase != "preparation") return "営業準備中に回答してください。";
            return accept ? DispatchReason(State.sato_dispatch.cat_id, SatoDestination) : "";
        }

        private void RespondSatoDispatch(string choice)
        {
            if (choice != "accept" && choice != "decline") throw new ArgumentException("指名依頼への回答を選んでください。");
            bool accept = choice == "accept";
            string reason = SatoDispatchResponseReason(accept);
            if (reason != "") throw new InvalidOperationException(reason);
            var request = State.sato_dispatch;
            if (!accept) { request.declined_day=State.day; return; }
            Dispatch(request.cat_id, SatoDestination);
            request.accepted_day=State.day;
            request.refused_return=DiscontentScore(Sato, State.day) >= Rules.discontent.threshold;
        }

        private void StartSatoMissing(CafeCat cat, string reason)
        {
            cat.activity="missing";
            cat.working=false;
            cat.kitten=false;
            cat.missing_remaining=Rules.missing_days;
            State.missing_cases.Add(new CafeMissingCase { id="missing-"+State.day+"-"+cat.id, cat_id=cat.id,
                departed_day=State.day, reason=reason });
        }

        private bool SatoRefusal(CafeSnapshot state, CafeDispatch e)
            => e.destination == SatoDestination && state.sato_dispatch?.refused_return == true;

        private void FinishSatoDispatch(CafeDispatch e, CafeCat cat)
        {
            if (e.destination != SatoDestination) return;
            if (SatoRefusal(State,e)) StartSatoMissing(cat,"sato_refused");
            else
            {
                State.sato_dispatch.returned_day=State.day;
                State.sato_dispatch.returned_after_service=State.phase=="closed";
                ReplaySatoReturn(State, State.discontent, State.day, State.sato_dispatch.returned_after_service);
            }
        }

        private void ReplaySatoReturn(CafeSnapshot s, List<CafeDiscontent> scores, int day, bool afterService)
        {
            var e = s.sato_dispatch;
            if (e?.accepted_day > 0 && !e.refused_return && e.returned_day == day && e.returned_after_service == afterService)
                ChangeDiscontent(scores, day, Sato, -Rules.discontent.good_service_recovery);
        }

        private void ValidateSatoDispatch(CafeSnapshot s)
        {
            if (s.sato_affinity == null || s.sato_affinity.Any(e => e == null || !s.cats.Any(c => c.id == e.cat_id)
                || !ValidSatoScene(e.conversation,"sato.affinity.v1","affinity",2))
                || s.sato_affinity.Select(e => e.cat_id).Distinct().Count() != s.sato_affinity.Count)
                throw new ArgumentException("Invalid Sato affinity scenes");

            var request = s.sato_dispatch;
            var trips = s.dispatches.Where(e => e.destination == SatoDestination).ToArray();
            CafeMissingDialogue.Validate(s);
            var missing = SatoMissing(s);
            if (s.missing_cases.Count(e => e.reason == "sato_expired" || e.reason == "sato_refused") > 1)
                throw new ArgumentException("Duplicate Sato missing case");
            if (request == null)
            {
                if (trips.Length != 0 || missing != null) throw new ArgumentException("Missing Sato request");
                return;
            }

            if (request.epilogue_read && !SatoEpilogueAvailable(s,request.cat_id))
                throw new ArgumentException("Sato epilogue read before unlock");
            var trip = trips.SingleOrDefault();
            if (!s.sato_affinity.Any(e => e.cat_id == request.cat_id) || request.requested_day < 2 || request.requested_day > s.day
                || request.accepted_day < 0 || request.accepted_day > s.day || request.declined_day < 0 || request.declined_day > s.day
                || request.returned_day < 0 || request.returned_day > s.day
                || !ValidSatoScene(request.conversation,"sato.request.v1","introduction",2)
                || (request.accepted_day == 0) != (trip == null) || (request.refused_return && trip == null)
                || (trip != null && (trip.cat_id != request.cat_id || request.declined_day != 0
                    || trip.started_day != request.accepted_day || trip.started_day < request.requested_day || trip.started_day >= request.requested_day + 2))
                || (request.declined_day != 0 && (missing != null || request.declined_day < request.requested_day))
                || (missing != null && (missing.cat_id != request.cat_id || missing.resolved_day != request.returned_day
                    || (request.refused_return ? missing.reason != "sato_refused" || trip.resolved_day != missing.departed_day
                        : trip != null || missing.reason != "sato_expired" || missing.departed_day < request.requested_day + 2)))
                || (missing != null && missing.resolved_day == 0 && !s.cats.Any(c => c.id == missing.cat_id
                    && c.activity == "missing" && !c.working && !c.kitten))
                || (trip?.status == "resolved" && request.refused_return && missing == null)
                || (missing == null && request.returned_day != (trip?.resolved_day ?? 0)))
                throw new ArgumentException("Invalid Sato dispatch request");
        }

        private static bool ValidSatoScene(CafeConversationProgress p, string id, string branch, int length)
            => p != null && p.conversation_id == id && p.branch_id == branch && p.node >= 0 && p.node <= length
                && p.page_offset >= 0 && p.page_offset <= 10000 && p.viewed_branches != null
                && (p.completed ? p.node == length && p.viewed_branches.SequenceEqual(new[]{branch}) : p.viewed_branches.Length == 0);
    }
}
