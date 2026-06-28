using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Task233.Editor
{
    public sealed class Task233PreviewWindow : EditorWindow
    {
        private const int ProbeIterations = 10000;
        private static readonly Action NoopAction = Noop;

        private Vector2 scroll;
        private string status = "Ready";
        private string allocationReport = "Run the allocation probe to preview warmed factory-path GC in the Editor.";
        private Task233CancelSource runningCancel;

        [MenuItem("Tools/Task233/Preview")]
        public static void Open()
        {
            var window = GetWindow<Task233PreviewWindow>("Task233");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnDisable()
        {
            if (runningCancel.IsCreated)
            {
                runningCancel.Cancel();
                runningCancel.Dispose();
                runningCancel = default;
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Task233 Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use this window to prewarm Task233, inspect editor-side allocation probes, and run a Play Mode delay/cancel preview.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Prewarm", GUILayout.Height(28)))
                {
                    T233.Prewarm(32768, 32768, 32768);
                    status = "Prewarmed continuation queues, delay nodes, and cancellation handles.";
                }

                if (GUILayout.Button("Allocation Probe", GUILayout.Height(28)))
                {
                    RunAllocationProbe();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = Application.isPlaying && !runningCancel.IsCreated;
                if (GUILayout.Button("Run Play Mode Preview", GUILayout.Height(28)))
                {
                    RunPlayModePreview();
                }

                GUI.enabled = runningCancel.IsCreated;
                if (GUILayout.Button("Cancel Preview", GUILayout.Height(28)))
                {
                    runningCancel.Cancel();
                }

                GUI.enabled = true;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run the actual await sequence preview. The allocation probe can run outside Play Mode.", MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(status, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(38));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Allocation Probe", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(allocationReport, GUILayout.MinHeight(180));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("API Preview", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "await T233.Yield();\n" +
                "await T233.DelayFrames(3);\n" +
                "await T233.DelaySeconds(0.25d);\n" +
                "await T233.DelayMilliseconds(16);\n" +
                "T233.Post(staticAction);",
                GUILayout.MinHeight(100));

            EditorGUILayout.EndScrollView();
        }

        private static void Noop()
        {
        }

        private void RunAllocationProbe()
        {
            T233.Prewarm(32768, 32768, 32768);

            var builder = new StringBuilder(512);
            builder.AppendLine("Warmed factory-path allocation probe");
            builder.AppendLine("Iterations: " + ProbeIterations);
            builder.AppendLine();
            AppendProbe(builder, "Yield awaitable factory", () => { _ = T233.Yield(); });
            AppendProbe(builder, "DelayFrames awaitable factory", () => { _ = T233.DelayFrames(1); });
            AppendProbe(builder, "DelaySeconds awaitable factory", () => { _ = T233.DelaySeconds(0.001d); });
            AppendProbe(builder, "DelayMilliseconds awaitable factory", () => { _ = T233.DelayMilliseconds(1); });
            AppendProbe(builder, "Cancel create/cancel/dispose", () =>
            {
                var cancel = T233.CreateCancelSource();
                cancel.Cancel();
                cancel.Dispose();
            });
            AppendProbe(builder, "Cached delegate access", () => { _ = NoopAction; });

            allocationReport = builder.ToString();
            status = "Allocation probe complete.";
        }

        private static void AppendProbe(StringBuilder builder, string name, Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < ProbeIterations; i++)
            {
                action();
            }

            var after = GC.GetAllocatedBytesForCurrentThread();
            var bytes = after - before;
            builder.Append(name);
            builder.Append(": ");
            builder.Append(bytes);
            builder.Append(" B total, ");
            builder.Append(bytes / (double)ProbeIterations);
            builder.AppendLine(" B/op");
        }

        private async void RunPlayModePreview()
        {
            runningCancel = T233.CreateCancelSource();
            status = "Running Play Mode preview...";

            try
            {
                await T233.Yield(cancellation: runningCancel);
                await T233.DelayFrames(2, cancellation: runningCancel);
                await T233.DelayMilliseconds(16, cancellation: runningCancel, ignoreTimeScale: true);
                await T233.DelaySeconds(0.05d, cancellation: runningCancel, ignoreTimeScale: true);
                T233.Post(NoopAction);
                status = "Play Mode preview completed.";
            }
            catch (OperationCanceledException)
            {
                status = "Play Mode preview canceled.";
            }
            finally
            {
                runningCancel.Dispose();
                runningCancel = default;
                Repaint();
            }
        }
    }
}
