using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactory.EditorTools
{
    internal sealed class PFDataCreateForm : VisualElement
    {
        private readonly List<Type> types;
        private readonly PopupField<string> typeField;
        private readonly TextField nameField;
        private readonly ObjectField folderField;
        private readonly HelpBox message;
        private readonly Button create;
        private readonly ScriptableObject template;

        public PFDataCreateForm(int category, ScriptableObject source, Action<ScriptableObject> completed, Action cancel)
        {
            name = "pf-create-form";
            AddToClassList("pf-create-form");
            template = source;
            types = PFDataCatalog.Types(category);
            Add(new Label(source == null ? "새 데이터 만들기" : "선택 데이터 복제") { name = "pf-create-title" });
            var labels = new List<string>();
            foreach (var type in types) labels.Add(PFDataCatalog.TypeLabel(type));
            int index = source == null ? 0 : Math.Max(0, types.IndexOf(source.GetType()));
            typeField = new PopupField<string>("종류", labels, index) { name = "pf-create-type" };
            typeField.SetEnabled(source == null && types.Count > 0);
            Add(typeField);
            nameField = new TextField("파일 이름") { name = "pf-create-name", value = source == null ? "NewData" : source.name + "_Copy" };
            Add(nameField);
            folderField = new ObjectField("저장 폴더") { name = "pf-create-folder", objectType = typeof(DefaultAsset), allowSceneObjects = false };
            string folder = source != null ? System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(source)).Replace('\\', '/') :
                category == 2 ? "Assets/6_Scripts/Data/SO/Credit" : "Assets/6_Scripts/Data/SO/Building";
            folderField.value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            Add(folderField);
            Add(new Label("폴더를 Project 창에서 끌어 놓으세요. 같은 파일 이름은 숫자를 붙여 저장합니다.") { name = "pf-create-hint" });
            message = new HelpBox("", HelpBoxMessageType.Warning) { name = "pf-create-validation" };
            Add(message);
            var actions = new VisualElement();
            actions.AddToClassList("pf-row");
            create = new Button(() =>
            {
                try
                {
                    var asset = PFDataCatalog.Create(CurrentType, nameField.value, Folder, template);
                    completed(asset);
                }
                catch (Exception ex)
                {
                    message.text = ex.Message;
                    message.style.display = DisplayStyle.Flex;
                }
            }) { text = source == null ? "생성" : "복제", name = "pf-create-confirm" };
            create.AddToClassList("pf-primary");
            actions.Add(create);
            actions.Add(new Button(cancel) { text = "취소", name = "pf-create-cancel" });
            Add(actions);
            typeField.RegisterValueChangedCallback(_ => ValidateForm());
            nameField.RegisterValueChangedCallback(_ => ValidateForm());
            folderField.RegisterValueChangedCallback(_ => ValidateForm());
            ValidateForm();
        }

        private Type CurrentType => types.Count == 0 ? null : types[typeField.index];
        private string Folder => AssetDatabase.GetAssetPath(folderField.value);

        private void ValidateForm()
        {
            string error = PFDataCatalog.Validate(CurrentType, nameField.value, Folder);
            message.text = error ?? "";
            message.style.display = error == null ? DisplayStyle.None : DisplayStyle.Flex;
            create.SetEnabled(error == null);
        }
    }
}
