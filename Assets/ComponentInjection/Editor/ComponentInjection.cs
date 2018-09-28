using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using UnityEditor.Callbacks;

public class ComponentInjection : Editor
{
    static List<GameObject> GetAllObjectsFromAllScenes()
    {
        List<GameObject> result = new List<GameObject>();

        int sceneCount = SceneManager.sceneCount;
        for (int sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            result.AddRange(scene.GetRootGameObjects());
        }

        return result;
    }

    static void ProcessReferences()
    {
        List<GameObject> allObjects = GetAllObjectsFromAllScenes();

        for (int gameObjectIndex = 0; gameObjectIndex < allObjects.Count; gameObjectIndex++)
        {
            GameObject gameObject = allObjects[gameObjectIndex];

            MonoBehaviour[] monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
            Undo.RecordObjects(monoBehaviours, "AutoComponentReference MonoBehaviours");

            for (int monoBehaviourIndex = 0; monoBehaviourIndex < monoBehaviours.Length; monoBehaviourIndex++)
            {
                MonoBehaviour monoBehaviour = monoBehaviours[monoBehaviourIndex];

                Type mbType = monoBehaviour.GetType();

                FieldInfo[] fieldInfos = mbType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (int fieldInfoIndex = 0; fieldInfoIndex < fieldInfos.Length; fieldInfoIndex++)
                {
                    FieldInfo fieldInfo = fieldInfos[fieldInfoIndex];

                    ComponentInjectionAttribute autoReferenceC = Attribute.GetCustomAttribute(fieldInfo, typeof(ComponentInjectionAttribute)) as ComponentInjectionAttribute;

                    if(autoReferenceC != null)
                    {
                        List<GameObject> autoComponentRefTargets = GetComponentTargets(gameObject, autoReferenceC.Type);

                        if(autoComponentRefTargets.Count > 0)
                        {
                            Type fieldType = fieldInfo.FieldType;

                            try
                            {
                                if(fieldType.IsArray)
                                {
                                    Type fieldElementType = fieldType.GetElementType();
                                    List<Component> components = new List<Component>();

                                    for (int targetIndex = 0; targetIndex < autoComponentRefTargets.Count; targetIndex++)
                                        components.AddRange(autoComponentRefTargets[targetIndex].GetComponents(fieldElementType));
                                    
                                    if(components.Count > 0)
                                    {
                                        Component[] componentsArray = components.ToArray();

                                        Array arrayObject = Array.CreateInstance(fieldElementType, componentsArray.Length);

                                        for (int componentIndex = 0; componentIndex < componentsArray.Length; componentIndex++)
                                            arrayObject.SetValue(componentsArray[componentIndex], componentIndex);

                                        fieldInfo.SetValue(monoBehaviour, arrayObject);
                                    }
                                    else
                                    {
                                        ACRLogErrorToFindRef(fieldType, monoBehaviour, fieldInfo);
                                    }
                                }
                                else if (fieldType.IsGenericType)
                                {
                                    if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
                                    {
                                        Type fieldElementType = fieldType.GetGenericArguments()[0];

                                        List<Component> components = new List<Component>();

                                        for (int targetIndex = 0; targetIndex < autoComponentRefTargets.Count; targetIndex++)
                                            components.AddRange(autoComponentRefTargets[targetIndex].GetComponents(fieldElementType));
                                        
                                        if (components.Count > 0)
                                        {
                                            Component[] componentsArray = components.ToArray();

                                            Type listOfElementsType = typeof(List<>).MakeGenericType(fieldElementType);

                                            IList list = Activator.CreateInstance(listOfElementsType) as IList;

                                            for (int componentIndex = 0; componentIndex < componentsArray.Length; componentIndex++)
                                                list.Add(componentsArray[componentIndex]);
                                            
                                            fieldInfo.SetValue(monoBehaviour, list);
                                        }
                                        else
                                        {
                                            ACRLogErrorToFindRef(fieldType, monoBehaviour, fieldInfo);
                                        }
                                    }
                                }
                                else
                                {
                                    // Find a component with a matching type to the field.
                                    Component cComponent = autoComponentRefTargets[0].GetComponent(fieldType);

                                    if (cComponent != null)
                                    {
                                        fieldInfo.SetValue(monoBehaviour, cComponent);
                                    }
                                    else
                                    {
                                        ACRLogErrorToFindRef(fieldType, monoBehaviour, fieldInfo);
                                    }
                                }
                            }
                            catch(ArgumentException e)
                            {
                                Debug.LogError("AutoComponentReference: The AutoComponent attribute is is being used on an incompatible type.\nType: " + fieldType.Name + "\n MonoBehaviour: " + monoBehaviour.name + "\n Field: " + fieldInfo.Name);
                            }
                        }
                        else
                        {
                            Debug.LogError("AutoComponentReference: No GameObjects were found for AutoComponentReference. Please make sure the AutoComponentAttribute attribute applied has a valid target type.\nMonoBehaviour: " + monoBehaviour.name + "\n Field: " + fieldInfo.Name);
                        }
                    }
                }
            }
        }     
    }

    static List<GameObject> GetComponentTargets(GameObject gameObject, ComponentInjectionType Type)
    {
        List<GameObject> result = new List<GameObject>();

        if((Type & ComponentInjectionType.Self) != ComponentInjectionType.Undefined)
            result.Add(gameObject);

        if ((Type & ComponentInjectionType.Parent) != ComponentInjectionType.Undefined)
            result.Add(gameObject.transform.parent.gameObject);

        if ((Type & ComponentInjectionType.Children) != ComponentInjectionType.Undefined)
            result.AddRange(GetChildObjects(gameObject));

        if ((Type & ComponentInjectionType.Siblings) != ComponentInjectionType.Undefined)
        {
            List<GameObject> siblings = GetChildObjects(gameObject.transform.parent.gameObject);
            for (int siblingIndex = 0; siblingIndex < siblings.Count; siblingIndex++)
            {
                GameObject sibling = siblings[siblingIndex];
                if (sibling != gameObject)
                    result.Add(sibling);
            }
        }

        if ((Type & ComponentInjectionType.Scene) != ComponentInjectionType.Undefined)
        {
            List<GameObject> allObjects = GetAllObjectsFromAllScenes();
            result.AddRange(allObjects);
        }

        return result;
    }

    static List<GameObject> GetChildObjects(GameObject gameObject)
    {
        List<GameObject> result = new List<GameObject>();

        Transform transform = gameObject.transform;
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            result.Add(child.gameObject);
        }

        return result;
    }

    static void ACRLogErrorToFindRef(Type fieldType, MonoBehaviour monoBehaviour, FieldInfo fieldInfo)
    {
        Debug.LogError("AutoComponentReference: Failed to find a reference to the type. Please ensure the AutoComponentAttribute attribute applied has a valid target type.\nType: " + fieldType.Name + "\n	MonoBehaviour: " + monoBehaviour.name + "]\n Field: [" + fieldInfo.Name + "]");
    }

    [PostProcessScene]
    static void OnPostProcessScene()
    {
        ProcessReferences();
    }

    [DidReloadScripts]
    static void OnOnPostProcessScene()
    {
        ProcessReferences();
    }
}
