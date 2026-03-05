using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 拥有者
/// </summary>
public enum OwnerType
{
    None,
    Player,
    Enemy,
}

/// <summary>
/// 卡堆类型
/// </summary>
public enum CardPileType
{
    Tiled, // 平铺
    Stack, // 堆叠
}
