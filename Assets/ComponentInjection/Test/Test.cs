using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField, ComponentInjection]
    Transform privateTrasform;

    [ComponentInjection]
    public Transform publicTransform;

    [SerializeField, ComponentInjection]
    Rigidbody privateRigidbody;

    [ComponentInjection]
    public Rigidbody publicRigidbody;

    [SerializeField, ComponentInjection]
    MeshRenderer privateMeshRenderer;

    [ComponentInjection]
    public MeshRenderer publicMeshRenderer;

    [SerializeField, ComponentInjection]
    Collider privateCollider;

    [ComponentInjection]
    public Collider publicCollider;

    [SerializeField, ComponentInjection(ComponentInjectionType.Children)]
    MeshFilter[] MyMeshFilter;
}
