using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Objective))]
public class ObjectiveInspector : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        var idField = new PropertyField(property.FindPropertyRelative("id"));
        var titleField = new PropertyField(property.FindPropertyRelative("title"));
        var descField = new PropertyField(property.FindPropertyRelative("description"), "Description");


        // PROGRESS BAR
        var currentAmountProp = property.FindPropertyRelative("currentAmount");
        var requiredAmountProp = property.FindPropertyRelative("requiredAmount");

        var progressBar = new ProgressBar();
        progressBar.lowValue = 0f;

        System.Action updateProgressBar = () =>
        {
            int req = requiredAmountProp.intValue;
            int cur = currentAmountProp.intValue;

            progressBar.highValue = req > 0 ? req : 1;
            progressBar.value = cur;
            progressBar.title = $"Progress: {cur} / {req}";
        };

        updateProgressBar();

        progressBar.TrackPropertyValue(currentAmountProp, _ => updateProgressBar());
        progressBar.TrackPropertyValue(requiredAmountProp, _ => updateProgressBar());


        // ADDIN STUFF
        container.Add(idField);
        container.Add(titleField);
        container.Add(descField);
        container.Add(progressBar);

        return container;
    }
}