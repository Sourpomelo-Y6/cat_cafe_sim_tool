using UnityEditor;
namespace CatCafePreview.Editor
{
    public static class DevAcquisitionSetup
    {
        [MenuItem("Cat Cafe/Development verification/Build isolated acquisition runner")]
        public static void Build()
        {
            // Uses the shared Pending -> Latest promotion, with no persistent PlayerSettings define.
            StandaloneBuildStorage.Build(StandaloneScreenSetup.Scenes,new[]{"CATCAFE_DEV_ACQUISITION"});
        }
    }
}
