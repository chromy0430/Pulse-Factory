using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodexHarness.Tests
{
    public sealed class CodexHarnessSceneTests
    {
        private GameObject root;
        private Scene fixtureScene;

        [SetUp]
        public void SetUp()
        {
            fixtureScene = EditorSceneManager.NewPreviewScene();
            root = new GameObject("__HarnessTest");
            SceneManager.MoveGameObjectToScene(root, fixtureScene);
            root.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (fixtureScene.IsValid())
                EditorSceneManager.ClosePreviewScene(fixtureScene);
        }

        [Test]
        public void OptionalNoneReference_IsAccepted()
        {
            root.AddComponent<HarnessReferenceProbe>();
            Assert.That(CodexHarnessSceneValidator.Inspect(root).Total, Is.Zero);
        }

        [Test]
        public void DeletedComponentReference_IsDetected()
        {
            var probe = root.AddComponent<HarnessReferenceProbe>();
            var collider = root.AddComponent<BoxCollider>();
            probe.optionalReference = collider;
            UnityEngine.Object.DestroyImmediate(collider);
            var report = CodexHarnessSceneValidator.Inspect(root);
            Assert.That(report.MissingReferences, Is.EqualTo(1));
            Assert.That(report.UnassignedReferences, Is.Zero);
        }

        [Test]
        public void HiddenDeletedReference_InInactiveChild_IsDetected()
        {
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            var probe = child.AddComponent<HarnessReferenceProbe>();
            var collider = child.AddComponent<BoxCollider>();
            probe.hiddenReference = collider;
            UnityEngine.Object.DestroyImmediate(collider);
            var report = CodexHarnessSceneValidator.Inspect(root);
            Assert.That(report.MissingReferences, Is.EqualTo(1));
            Assert.That(report.Issues[0], Does.Contain("__HarnessTest/Child"));
            Assert.That(report.Issues[0], Does.Contain("hiddenReference"));
        }

        [Test]
        public void MissingScriptSlot_IsDetected()
        {
            var probe = root.AddComponent<HarnessReferenceProbe>();
            using (var serialized = new SerializedObject(probe))
            {
                serialized.FindProperty("m_Script").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.EqualTo(1),
                "Fixture must contain a real Missing Script slot.");
            Assert.That(CodexHarnessSceneValidator.Inspect(root).MissingScripts, Is.EqualTo(1));
        }

        [Test]
        public void ExistingOdinRequiredContract_IsDetected()
        {
            Type type = Type.GetType("BuildController, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, "Pulse Factory BuildController must exist.");
            Assert.That(CodexHarnessReferenceRules.IsRequired(type, "mainCamera"), Is.True);
            root.AddComponent(type);
            var report = CodexHarnessSceneValidator.Inspect(root);
            Assert.That(report.UnassignedReferences, Is.GreaterThan(0));
            Assert.That(report.Issues.Exists(issue => issue.Contains("BuildController.mainCamera")), Is.True);
        }
    }
}
