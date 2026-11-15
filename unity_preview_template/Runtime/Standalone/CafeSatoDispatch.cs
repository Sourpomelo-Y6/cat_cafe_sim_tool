using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeSatoDispatch
    {
        public string cat_id;
        public int return_access, requested_day, accepted_day, declined_day, returned_day;
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
        public const string Suzuki = "guest-2";
        public const string SuzukiDestination = "suzuki_request";
        public static readonly IReadOnlyList<string> NamedDispatchCustomers = Array.AsReadOnly(new[]{Sato,Suzuki});
        public static string NamedCustomerName(string customer) => customer==Suzuki?"鈴木さん":"佐藤さん";
        public static string NamedDestinationCustomer(string destination)
            => destination==SatoDestination?Sato:destination==SuzukiDestination?Suzuki:null;
        public static string NamedMissingCustomer(string reason)
            => reason=="sato_expired" || reason=="sato_refused"?Sato:
                reason=="suzuki_expired" || reason=="suzuki_refused"?Suzuki:null;
        private static string NamedPrefix(string customer) => customer==Suzuki?"suzuki":"sato";
        private static string NamedAdoptionSource(string customer) => NamedPrefix(customer)+"-return";
        public static bool IsNamedAdoption(string source) => source==SatoAdoptionSource || source=="suzuki-return";
        internal static readonly DispatchRules SuzukiVisit = new DispatchRules
            { id=SuzukiDestination, name="鈴木さんへの指名派遣", days=1, max_fatigue=60 };
        public CafeSatoDispatch NamedRequest(string customer) => NamedRequest(State,customer);
        private static CafeSatoDispatch NamedRequest(CafeSnapshot state,string customer)
            => customer==Sato?state.sato_dispatch:customer==Suzuki?state.suzuki_dispatch:null;
        public List<CafeSatoAffinityScene> NamedAffinity(string customer) => NamedAffinity(State,customer);
        private static List<CafeSatoAffinityScene> NamedAffinity(CafeSnapshot state,string customer)
            => customer==Suzuki?state.suzuki_affinity:state.sato_affinity;
        public const string SatoDestination = "sato_request";
        private const double SatoAffinity = 80;
        private const double SatoDiscontent = 50;
        internal static readonly DispatchRules SatoVisit = new DispatchRules
            { id=SatoDestination, name="佐藤さんへの指名派遣", days=1, max_fatigue=60 };

        private static CafeConversationProgress SatoScene(string id, string branch)
            => new CafeConversationProgress { conversation_id=id, branch_id=branch, viewed_branches=Array.Empty<string>() };

        private void ObserveSatoAffinity(string catId, string customer, double affinity)
        {
            if (State.import_history != null || !NamedDispatchCustomers.Contains(customer) || affinity < SatoAffinity || NamedAffinity(customer).Any(e => e.cat_id == catId)) return;
            NamedAffinity(customer).Add(new CafeSatoAffinityScene { cat_id=catId, conversation=SatoScene(NamedPrefix(customer)+".affinity.v1","affinity") });
        }

        private void PresentSatoDispatch()
        {
            foreach(var customer in NamedDispatchCustomers)PresentNamedDispatch(customer);
        }

        private void PresentNamedDispatch(string customer)
        {
            if (State.import_history != null) return;
            var request = NamedRequest(customer);
            if (request != null)
            {
                if (State.day == request.requested_day + 1 && request.accepted_day == 0 && request.declined_day == 0)
                    request.conversation = SatoScene(NamedPrefix(customer)+".request.v1","introduction");
                if (request.accepted_day != 0 || request.declined_day != 0 || State.day < request.requested_day + 2
                    || NamedMissing(State,customer) != null) return;
                var cat = State.cats.Single(c => c.id == request.cat_id);
                if (cat.activity == "adopted") request.declined_day = State.day;
                else if (cat.activity == "cafe") StartSatoMissing(cat, NamedPrefix(customer)+"_expired");
                return;
            }

            if (DiscontentScore(customer, State.day) < SatoDiscontent
                || State.trust_customers.Any(c => c.customer_id == customer && c.status != "stable")
                || State.adoption?.events.Any(e => e.customer_id == customer && e.status == "waiting") == true) return;
            var candidate = State.relationships
                .Where(r => r.customer_id == customer && r.affinity >= SatoAffinity && NamedAffinity(customer).Any(e => e.cat_id == r.cat_id))
                .Where(r => !NamedDispatchCustomers.Any(other => NamedRequest(other) is CafeSatoDispatch active
                    && active.cat_id==r.cat_id && active.returned_day==0 && active.declined_day==0))
                .Where(r => State.adoption?.events.Any(e=>e.cat_id==r.cat_id && e.status=="waiting") != true)
                .Where(r => State.cats.Any(c => c.id == r.cat_id && c.activity == "cafe" && c.health == "healthy"
                    && !c.cannot_continue && c.stamina > 0 && c.fatigue <= SatoVisit.max_fatigue))
                .OrderByDescending(r => r.affinity).ThenBy(r => r.cat_id, StringComparer.Ordinal).FirstOrDefault();
            if (candidate == null) return;
            if (State.cats.Count(c => c.activity == "cafe" && c.health == "healthy" && !c.cannot_continue && c.stamina > 0) <= State.seat_count) return;
            request = new CafeSatoDispatch { cat_id=candidate.cat_id, requested_day=State.day,
                conversation=SatoScene(NamedPrefix(customer)+".request.v1","introduction") };
            if(customer==Sato)State.sato_dispatch=request;else State.suzuki_dispatch=request;
        }

        // Zero identifies returns resolved before this choice was introduced.
        public const int AwaitingReturnAccess=1, AllowReturnAccess=2, BanReturnAccess=3;
        internal bool LegacyNamedReturnReplay;
        private const int ReturnTrustDelay=8;
        private const double ReturnTrustDiscontent=50;

        public bool NamedReturnAccessWaiting => NamedDispatchCustomers.Any(c=>NamedReturnAccessPending(c));

        public bool NamedReturnAccessPending(string customer)
            => NamedRequest(customer)?.return_access==AwaitingReturnAccess;

        private void ResolveNamedReturnAccess(string customer,string choice)
        {
            var request=NamedRequest(customer);
            if(choice!="allow" && choice!="ban")throw new ArgumentException("今後の来店を選んでください。");
            int access=choice=="allow"?AllowReturnAccess:BanReturnAccess;
            if(request?.return_access==access)return;
            if(request?.return_access!=AwaitingReturnAccess || request.returned_day!=State.day)
                throw new InvalidOperationException("回答待ちの帰還案件を選んでください。");
            request.return_access=access;
            State.revision++;
        }

        private bool NamedCustomerBanned(string customer,int day)
            => NamedRequest(customer) is CafeSatoDispatch e && e.return_access==BanReturnAccess && day>=e.returned_day;

        internal static bool UsesReturnTrustRules(CafeSnapshot s,CafeTrustEvent e)
            => ReturnTrustDay(s,e.customer_id) is int day && day>0 && e.day>=day;

        internal static bool IsNamedReturnTrustEvent(CafeSnapshot s,CafeTrustEvent e)
            => ReturnTrustDay(s,e.customer_id)==e.day;

        private static int ReturnTrustDay(CafeSnapshot s,string customer)
        {
            var e=NamedRequest(s,customer);
            return e?.return_access==AllowReturnAccess?e.returned_day+ReturnTrustDelay:0;
        }

        private static int OrdinaryTrustDay(CafeSnapshot s,CafeDiscontent d,int due)
        {
            if(due==int.MaxValue)return due;
            var e=NamedRequest(s,d.customer_id);
            if(e==null || due<e.requested_day)return due;
            if(e.return_access==BanReturnAccess || e.return_access==AwaitingReturnAccess)return int.MaxValue;
            int recheck=ReturnTrustDay(s,d.customer_id);
            if(recheck==0)return SatoTrustDay(s,d.customer_id,due);
            // Ignore old stops after the return check or a later successful recovery.
            return d.suspensions.Where(stop=>stop.day>=Math.Max(recheck,due-1))
                .Select(stop=>stop.day+1).DefaultIfEmpty(int.MaxValue).Min();
        }

        public const string SatoAdoptionSource = "sato-return";

        private void PresentSatoAdoption()
        {
            foreach(var customer in NamedDispatchCustomers)PresentNamedAdoption(customer);
        }

        private void PresentNamedAdoption(string customer)
        {
            var missing = NamedMissing(State,customer);
            if (missing == null || missing.resolved_day == 0 || missing.resolved_day + 1 != State.day
                || State.adoption?.enabled != true || NamedRequest(customer).return_access>=BanReturnAccess
                || NamedReturnAccessPending(customer)) return;
            var cat = State.cats.Single(c => c.id == missing.cat_id);
            if (cat.activity != "cafe") return;
            double affinity = State.relationships.Find(r => r.cat_id == cat.id && r.customer_id == customer)?.affinity ?? 0;
            double player = PlayerCat(cat.id).affinity;
            if (affinity < Math.Max(80,Rules.adoption_threshold) || affinity <= player || player > 40) return;
            State.adoption.events.Add(new CafeAdoptionEvent { id="adoption-"+State.day+"-"+cat.id,
                cat_id=cat.id, customer_id=customer, source=NamedAdoptionSource(customer), day=State.day, tick=0,
                guest_affinity=affinity, player_affinity=player, threshold=Rules.adoption_threshold });
        }

        public bool SatoEpilogueAvailable(string catId) => NamedEpilogueCustomer(catId)!=null;
        public string NamedEpilogueCustomer(string catId)
            => NamedDispatchCustomers.FirstOrDefault(c=>NamedEpilogueAvailable(State,catId,c));

        private static bool NamedEpilogueAvailable(CafeSnapshot s,string catId,string customer)
            => s.adoption?.events.Any(e => e.source == NamedAdoptionSource(customer) && e.customer_id==customer && e.cat_id == catId
                && e.status == "resolved" && e.choice == "accept" && s.day - e.resolved_day >= 3) == true;

        private static CafeMissingCase NamedMissing(CafeSnapshot state,string customer)
            => state.missing_cases?.SingleOrDefault(e => NamedMissingCustomer(e.reason)==customer);

        private static int SatoTrustDay(CafeSnapshot state, string customer, int day)
        {
            var e = NamedRequest(state,customer);
            if (e == null || day < e.requested_day || day == int.MaxValue) return day;
            int end = Math.Max(e.returned_day, e.declined_day);
            return end == 0 ? int.MaxValue : Math.Max(day, end + 1);
        }

        public bool NamedDispatchAwaitingAnswer(string customer) => NamedRequest(customer) is CafeSatoDispatch e && e.accepted_day == 0
            && e.declined_day == 0 && State.day < e.requested_day + 2;

        public string SatoDispatchResponseReason(bool accept,string customer=Sato)
        {
            if (!NamedDispatchAwaitingAnswer(customer)) return "この依頼の回答期限は終了しました。";
            if (State.game_over != null || State.phase != "preparation") return "営業準備中に回答してください。";
            return accept ? DispatchReason(NamedRequest(customer).cat_id, customer==Suzuki?SuzukiDestination:SatoDestination) : "";
        }

        private void RespondSatoDispatch(string choice,string customer)
        {
            if (choice != "accept" && choice != "decline") throw new ArgumentException("指名依頼への回答を選んでください。");
            bool accept = choice == "accept";
            string reason = SatoDispatchResponseReason(accept,customer);
            if (reason != "") throw new InvalidOperationException(reason);
            var request = NamedRequest(customer);
            if (!accept) { request.declined_day=State.day; return; }
            Dispatch(request.cat_id, customer==Suzuki?SuzukiDestination:SatoDestination);
            request.accepted_day=State.day;
            request.refused_return=DiscontentScore(customer, State.day) >= Rules.discontent.threshold;
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
            => NamedRequest(state,NamedDestinationCustomer(e.destination))?.refused_return == true;

        private void FinishSatoDispatch(CafeDispatch e, CafeCat cat)
        {
            string customer=NamedDestinationCustomer(e.destination);
            if (customer==null) return;
            var request=NamedRequest(customer);
            if (SatoRefusal(State,e)) StartSatoMissing(cat,NamedPrefix(customer)+"_refused");
            else
            {
                request.returned_day=State.day;
                request.returned_after_service=State.phase=="closed";
                ChangeDiscontent(State.discontent,State.day,customer,-Rules.discontent.good_service_recovery);
            }
        }

        private void ReplaySatoReturn(CafeSnapshot s, List<CafeDiscontent> scores, int day, bool afterService)
        {
            foreach(var customer in NamedDispatchCustomers)
            {
                var e=NamedRequest(s,customer);
                if (e?.accepted_day > 0 && !e.refused_return && e.returned_day == day && e.returned_after_service == afterService)
                    ChangeDiscontent(scores, day, customer, -Rules.discontent.good_service_recovery);
            }
        }

        private void ValidateSatoDispatch(CafeSnapshot s)
        {
            CafeMissingDialogue.Validate(s);
            foreach(var customer in NamedDispatchCustomers)ValidateNamedDispatch(s,customer);
            var active=NamedDispatchCustomers.Select(c=>NamedRequest(s,c)).Where(e=>e!=null && e.returned_day==0 && e.declined_day==0).ToArray();
            if(active.Length==2 && active[0].cat_id==active[1].cat_id)throw new ArgumentException("Overlapping named requests");
        }

        private void ValidateNamedDispatch(CafeSnapshot s,string customer)
        {
            if (NamedAffinity(s,customer) == null || NamedAffinity(s,customer).Any(e => e == null || !s.cats.Any(c => c.id == e.cat_id)
                || !ValidSatoScene(e.conversation,NamedPrefix(customer)+".affinity.v1","affinity",2))
                || NamedAffinity(s,customer).Select(e => e.cat_id).Distinct().Count() != NamedAffinity(s,customer).Count)
                throw new ArgumentException("Invalid Sato affinity scenes");

            var request = NamedRequest(s,customer);
            var trips = s.dispatches.Where(e => NamedDestinationCustomer(e.destination)==customer).ToArray();
            if (s.missing_cases.Count(e => NamedMissingCustomer(e.reason)==customer) > 1)
                throw new ArgumentException("Duplicate named missing case");
            var missing = NamedMissing(s,customer);
            if (request == null)
            {
                if (trips.Length != 0 || missing != null) throw new ArgumentException("Missing Sato request");
                return;
            }

            if (request.epilogue_read && !NamedEpilogueAvailable(s,request.cat_id,customer))
                throw new ArgumentException("Sato epilogue read before unlock");
            if(request.return_access<0 || request.return_access>BanReturnAccess
                || request.return_access!=0 && (missing==null || missing.resolved_day==0)
                || request.return_access==AwaitingReturnAccess && request.returned_day!=s.day)
                throw new ArgumentException("Invalid access decision for named return");
            var trip = trips.SingleOrDefault();
            if (!NamedAffinity(s,customer).Any(e => e.cat_id == request.cat_id) || request.requested_day < 2 || request.requested_day > s.day
                || request.accepted_day < 0 || request.accepted_day > s.day || request.declined_day < 0 || request.declined_day > s.day
                || request.returned_day < 0 || request.returned_day > s.day
                || !ValidSatoScene(request.conversation,NamedPrefix(customer)+".request.v1","introduction",2)
                || (request.accepted_day == 0) != (trip == null) || (request.refused_return && trip == null)
                || (trip != null && (trip.cat_id != request.cat_id || request.declined_day != 0
                    || trip.started_day != request.accepted_day || trip.started_day < request.requested_day || trip.started_day >= request.requested_day + 2))
                || (request.declined_day != 0 && (missing != null || request.declined_day < request.requested_day))
                || (missing != null && (missing.cat_id != request.cat_id || missing.resolved_day != request.returned_day
                    || (request.refused_return ? missing.reason != NamedPrefix(customer)+"_refused" || trip.resolved_day != missing.departed_day
                        : trip != null || missing.reason != NamedPrefix(customer)+"_expired" || missing.departed_day < request.requested_day + 2)))
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
