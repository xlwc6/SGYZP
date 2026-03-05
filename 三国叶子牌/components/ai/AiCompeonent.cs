using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI组件
/// </summary>
public partial class AiCompeonent : Node
{
    private HandManager enemyHandManager;
    private CardPile enemyCardPile;
    private CardPile playerCardPile;

    private Dictionary<string, string> allCardRes = [];// 全部的卡牌
    private List<string> usedCard = []; // 已知的卡牌集合
    private List<CardData> handCards = [];// 自己的手牌
    private List<CardData> playedCards = [];// 已经使用过的牌
    private List<CardData> playerPossibleCards = [];// 玩家可能的手牌

    public override void _Ready()
    {
        /*
         * 敌人AI分析
         * 1、检索自身的手牌
         * 2、拥有记牌器，可以记录自己和玩家已经使用过的牌
         * 3、本身有2种决策，每次出牌时根据当前双方血量和手牌分数预估来判断使用哪种决策
         * 4、博弈论策略：把手牌按武力作为分值标准，得出当前手牌强度分数
         * 5、模拟策略：把未出现的牌与手牌每个牌模拟对战，使用胜率最高的那张
         */
        enemyHandManager = GetTree().GetFirstNodeInGroup("enemy_deck") as HandManager;
        enemyCardPile = GetTree().GetFirstNodeInGroup("enemy_discard_deck") as CardPile;
        playerCardPile = GetTree().GetFirstNodeInGroup("player_discard_deck") as CardPile;

        InitCardRes();
    }

    private void InitCardRes()
    {
        allCardRes.Clear();
        // 所以的卡牌数据应该以JSON或者其他格式存储地址，这里由于游戏体量小就无所谓
        allCardRes.Add("刘协", "res://resources/card_data/qun/刘协.tres");
        allCardRes.Add("吕布", "res://resources/card_data/qun/吕布.tres");
        allCardRes.Add("张角", "res://resources/card_data/qun/张角.tres");
        allCardRes.Add("文丑", "res://resources/card_data/qun/文丑.tres");
        allCardRes.Add("袁绍", "res://resources/card_data/qun/袁绍.tres");
        allCardRes.Add("貂蝉", "res://resources/card_data/qun/貂蝉.tres");
        allCardRes.Add("颜良", "res://resources/card_data/qun/颜良.tres");
        allCardRes.Add("关羽", "res://resources/card_data/shu/关羽.tres");
        allCardRes.Add("刘备", "res://resources/card_data/shu/刘备.tres");
        allCardRes.Add("姜维", "res://resources/card_data/shu/姜维.tres");
        allCardRes.Add("张飞", "res://resources/card_data/shu/张飞.tres");
        allCardRes.Add("法正", "res://resources/card_data/shu/法正.tres");
        allCardRes.Add("诸葛亮", "res://resources/card_data/shu/诸葛亮.tres");
        allCardRes.Add("赵云", "res://resources/card_data/shu/赵云.tres");
        allCardRes.Add("马超", "res://resources/card_data/shu/马超.tres");
        allCardRes.Add("黄忠", "res://resources/card_data/shu/黄忠.tres");
        allCardRes.Add("典韦", "res://resources/card_data/wei/典韦.tres");
        allCardRes.Add("司马懿", "res://resources/card_data/wei/司马懿.tres");
        allCardRes.Add("夏侯渊", "res://resources/card_data/wei/夏侯渊.tres");
        allCardRes.Add("张辽", "res://resources/card_data/wei/张辽.tres");
        allCardRes.Add("曹操", "res://resources/card_data/wei/曹操.tres");
        allCardRes.Add("许褚", "res://resources/card_data/wei/许褚.tres");
        allCardRes.Add("郭嘉", "res://resources/card_data/wei/郭嘉.tres");
        allCardRes.Add("周泰", "res://resources/card_data/wu/周泰.tres");
        allCardRes.Add("周瑜", "res://resources/card_data/wu/周瑜.tres");
        allCardRes.Add("太史慈", "res://resources/card_data/wu/太史慈.tres");
        allCardRes.Add("孙权", "res://resources/card_data/wu/孙权.tres");
        allCardRes.Add("甘宁", "res://resources/card_data/wu/甘宁.tres");
        allCardRes.Add("陆逊", "res://resources/card_data/wu/陆逊.tres");
        allCardRes.Add("鲁肃", "res://resources/card_data/wu/鲁肃.tres");
    }

    /// <summary>
    /// 初始化AI掌握的卡牌数据
    /// </summary>
    public void InitCardData()
    {
        usedCard.Clear();
        handCards.Clear();
        playedCards.Clear();
        playerPossibleCards.Clear();
        // 重新计算当前卡牌数据
        foreach (var item in enemyHandManager.cards)
        {
            handCards.Add(item.cardData);
            usedCard.Add(item.cardData.Name);
        }
        foreach (var item in enemyCardPile.cards)
        {
            playedCards.Add(item.cardData);
            usedCard.Add(item.cardData.Name);
        }
        foreach (var item in playerCardPile.cards)
        {
            playedCards.Add(item.cardData);
            usedCard.Add(item.cardData.Name);
        }
        foreach (var item in allCardRes.Keys)
        {
            if (usedCard.Contains(item))
            {
                continue;
            }
            var cardData = ResourceLoader.Load<CardData>(allCardRes[item]);
            playerPossibleCards.Add(cardData);
        }
    }

    /// <summary>
    /// 手牌强度评估
    /// </summary>
    /// <returns></returns>
    public float EvaluateHandStrength()
    {


        return 0;
    }
}
