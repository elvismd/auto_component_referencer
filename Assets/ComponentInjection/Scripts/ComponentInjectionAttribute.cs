using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ComponentInjectionType
{
    Undefined = 0,
    Self = 1,
    Parent = 2,
    Children = 4,
    Siblings = 8,
    Scene = 16,
}

[AttributeUsage(AttributeTargets.Field)]
public class ComponentInjectionAttribute : Attribute
{
    public ComponentInjectionType Type;

    public ComponentInjectionAttribute()
    {
        Type = ComponentInjectionType.Self;
    }

    public ComponentInjectionAttribute(ComponentInjectionType Type)
    {
        this.Type = Type;
    }
}
