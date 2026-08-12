using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityObjectLink.Tests
{
    public sealed class UnityObjectLinkGlobalObjectIdTests
    {
        private const string TestRoot = "Assets/UnityObjectLinkGeneratedTests";

        private sealed class TestAsset : ScriptableObject
        {
            public string value;
        }

        private sealed class RecordingSelectionService : IUnityObjectLinkSelectionService
        {
            internal Object Target;

            public void SelectAndPing(Object target)
            {
                Target = target;
            }
        }

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.CreateFolder("Assets", "UnityObjectLinkGeneratedTests");
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void AssetAndSubAsset_RoundTrip()
        {
            TestAsset main = ScriptableObject.CreateInstance<TestAsset>();
            TestAsset child = ScriptableObject.CreateInstance<TestAsset>();
            string path = TestRoot + "/asset.asset";
            AssetDatabase.CreateAsset(main, path);
            AssetDatabase.AddObjectToAsset(child, main);
            AssetDatabase.SaveAssets();

            AssertRoundTrip(main);
            AssertRoundTrip(child);
        }

        [Test]
        public void PrefabChild_RoundTrips()
        {
            var root = new GameObject("Root");
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            string path = TestRoot + "/object.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            AssertRoundTrip(prefab.transform.GetChild(0).gameObject);
        }

        [Test]
        public void SavedLoadedSceneObject_RoundTrips()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var target = new GameObject("Saved Scene Object");
            Assert.That(EditorSceneManager.SaveScene(scene, TestRoot + "/scene.unity"), Is.True);
            AssertRoundTrip(target);
        }

        [Test]
        public void SavedSceneComponent_RoundTrips()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var target = new GameObject("Component Owner").AddComponent<BoxCollider>();
            Assert.That(EditorSceneManager.SaveScene(scene, TestRoot + "/component-scene.unity"), Is.True);
            AssertRoundTrip(target);
        }

        [Test]
        public void UnloadedSceneObject_DoesNotResolveAndIsNotOpened()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var target = new GameObject("Unloaded Scene Object");
            string path = TestRoot + "/unloaded.unity";
            Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(target);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id), Is.Null);
            Assert.That(SceneManager.GetSceneByPath(path).isLoaded, Is.False);
        }

        [Test]
        public void UnsavedSceneObject_CannotCreateLink()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var target = new GameObject("Unsaved");
            string uri;
            string error;
            Assert.That(UnityObjectLinkApi.TryCreateLink(target, out uri, out error), Is.False);
            Assert.That(error, Does.Contain("unsaved Scene"));
        }

        [Test]
        public void NewObjectInPreviouslySavedDirtyScene_CannotCreateLink()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(EditorSceneManager.SaveScene(scene, TestRoot + "/dirty-scene.unity"), Is.True);
            var target = new GameObject("Not Saved Yet");
            EditorSceneManager.MarkSceneDirty(scene);
            string uri;
            string error;

            Assert.That(scene.isDirty, Is.True);
            Assert.That(UnityObjectLinkApi.TryCreateLink(target, out uri, out error), Is.False);
            Assert.That(error, Does.Contain("unsaved changes"));
        }

        [Test]
        public void DeletedAsset_DoesNotResolve()
        {
            TestAsset asset = ScriptableObject.CreateInstance<TestAsset>();
            string path = TestRoot + "/deleted.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(asset);

            Assert.That(AssetDatabase.DeleteAsset(path), Is.True);
            Assert.That(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id), Is.Null);
        }

        [Test]
        public void Resolver_UsesReplaceableSelectionBoundary()
        {
            TestAsset asset = ScriptableObject.CreateInstance<TestAsset>();
            string path = TestRoot + "/selected.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            string uri;
            string error;
            Assert.That(UnityObjectLinkApi.TryCreateLink(asset, out uri, out error), Is.True, error);
            var selection = new RecordingSelectionService();

            UnityObjectLinkResult result = UnityObjectLinkResolver.Handle(uri, selection, false);

            Assert.That(result.Status, Is.EqualTo(UnityObjectLinkStatus.Success));
            Assert.That(selection.Target, Is.EqualTo(asset));
        }

        [Test]
        public void Resolver_RejectsCorruptGlobalObjectId()
        {
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            string uri = settings.Scheme + "://select?v=1&project=" + settings.ProjectId + "&object=GlobalObjectId_V1-not-valid";
            var selection = new RecordingSelectionService();

            UnityObjectLinkResult result = UnityObjectLinkResolver.Handle(uri, selection, false);

            Assert.That(result.Status, Is.EqualTo(UnityObjectLinkStatus.InvalidUri));
            Assert.That(selection.Target, Is.Null);
        }

        private static void AssertRoundTrip(Object target)
        {
            string uri;
            string error;
            Assert.That(UnityObjectLinkApi.TryCreateLink(target, out uri, out error), Is.True, error);
            UnityObjectLinkUri parsed;
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            Assert.That(UnityObjectLinkUri.TryParse(uri, settings.Scheme, settings.ProjectId, out parsed, out error), Is.True, error);

            GlobalObjectId id;
            Assert.That(GlobalObjectId.TryParse(parsed.GlobalObjectId, out id), Is.True);
            Assert.That(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id), Is.EqualTo(target));
        }
    }
}
