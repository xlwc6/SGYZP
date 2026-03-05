using Godot;
using System;

/// <summary>
/// 牌堆类
/// </summary>
public interface IDeck
{
    public void AddCard(Card card);
    public void RemoveCard(Card card);
    public void Clear();
    public Vector2 GetMarkPosition();
}
