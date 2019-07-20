﻿using Oak.Valkyrie.AST.Term;

namespace Oak.Valkyrie.AST.Declaration;

/// <summary>
/// 一个虚拟的用于标记元属性的节点
/// </summary>
/// <para>支持泛型：</para>
/// <code>
/// #? document line1
/// #? document line2
/// [attr1, attr2]
/// [attr3()]
/// mod1 mod2 class ClassName
/// {
///
/// }
/// </code>
public sealed record Annotations
{
    /// <summary>
    ///     文档注释列表
    /// </summary>
    public IReadOnlyList<DocumentComment> Documents { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeList> AttributeLists { get; init; } = [];

    /// <summary>
    ///     修饰符列表（如 <c>public</c>、<c>abstract</c>）
    /// </summary>
    public IReadOnlyList<IdentifierNode> Modifiers { get; init; } = [];

    /// <summary>
    ///     扁平化后的属性项列表
    /// </summary>
    public IReadOnlyList<AttributeItem> Attributes()
    {
        if (AttributeLists.Count == 0)
        {
            return [];
        }

        var items = new List<AttributeItem>();
        foreach (var attributeList in AttributeLists)
        {
            if (attributeList.Items.Count > 0)
            {
                items.AddRange(attributeList.Items);
            }
        }

        return items;
    }

    public IReadOnlyList<string> ModifierTexts()
    {
        if (Modifiers.Count == 0)
        {
            return [];
        }

        var names = new List<string>(Modifiers.Count);
        foreach (var modifier in Modifiers)
        {
            names.Add(modifier.Name);
        }

        return names;
    }


    public string DocumentText()
    {
        return string.Join(Environment.NewLine, Documents);
    }
}
