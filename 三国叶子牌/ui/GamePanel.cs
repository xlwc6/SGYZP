using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 游戏面板
/// </summary>
public partial class GamePanel : Control
{
    private const int roundTick = 15; // 每回合的时间，秒
    private const int startCardNum = 10; // 初始化手牌数量
    private const float takeDuration = 0.5f; // 模拟抽卡的耗时
    private const float moveDuration = 1.0f; // 移动卡牌的耗时

    private GameEvent gameEvent;
    private HandManager enemyHandManager;
    private HandManager playerHandManager;
    private CardPile enemyCardPile;
    private CardPile playerCardPile;
    private PanelContainer battleLayer;
    private VBoxContainer turnLayer;
    private PanelContainer leftCard;
    private PanelContainer rightCard;
    private Label roundLabel;
    private Timer roundTimer;
    private Label timerLabel;
    private Control startLayer;
    private Button startBtn;
    private GameOverScene endLayer;
    private GamePausedScene pausedLayer;
    private CanvasLayer skillLayer;
    private Marker2D enemyPos;
    private Marker2D playerPos;

    // 游戏回合数据
    private WeightedTable<string> cardTable = new WeightedTable<string>("卡牌池");
    private string cardScene = "res://ui/card.tscn";
    private bool isGameOver = false; // 游戏是否结束
    private int currentRound = 0; // 当前回合数
    private int currentRoundTick = 0; // 当前回合耗时
    private List<BuffInfo> buffs = [];
    // 攻击技能数据
    private string skillHMSScene = "res://scenes/special_effects/half_moon_slash.tscn";
    private string skillIMPtScene = "res://scenes/special_effects/impact_bullet.tscn";

    public override void _Ready()
    {
        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        enemyHandManager = GetTree().GetFirstNodeInGroup("enemy_deck") as HandManager;
        playerHandManager = GetTree().GetFirstNodeInGroup("player_deck") as HandManager;
        enemyCardPile = GetTree().GetFirstNodeInGroup("enemy_discard_deck") as CardPile;
        playerCardPile = GetTree().GetFirstNodeInGroup("player_discard_deck") as CardPile;
        battleLayer = GetNode<PanelContainer>("%战斗面板");
        turnLayer = GetNode<VBoxContainer>("%回合信息");
        leftCard = GetNode<PanelContainer>("%左卡牌");
        rightCard = GetNode<PanelContainer>("%右卡牌");
        roundLabel = GetNode<Label>("%回合数");
        roundTimer = GetNode<Timer>("%回合计时器");
        timerLabel = GetNode<Label>("%倒计时");
        startLayer = GetNode<Control>("%准备界面");
        startBtn = GetNode<Button>("%开始游戏");
        endLayer = GetNode<GameOverScene>("%结束界面");
        pausedLayer = GetNode<GamePausedScene>("%暂停界面");
        skillLayer = GetNode<CanvasLayer>("技能层");
        enemyPos = GetNode<Marker2D>("%敌方位置");
        playerPos = GetNode<Marker2D>("%玩家位置");

        InitCardTable();
        InitDeckPanel();

        roundTimer.Timeout += RoundTimer_Timeout;
        startBtn.Pressed += StartGame;  

        // 连接卡牌信号
        gameEvent.Connect(GameEvent.SignalName.CardPlayed, Callable.From<Card>(OnCardPlayed));
        gameEvent.Connect(GameEvent.SignalName.Death, Callable.From<int>(OnDeathed));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            pausedLayer.ShowPause();
        }
    }

    private void StartGame()
    {
        pausedLayer.ClearLogs();
        startLayer.Visible = false;
        // 发牌
        HandOutCards();
    }

    private void RoundTimer_Timeout()
    {
        // 每次更新倒计时显示和判断是否都已出牌
        currentRoundTick++;
        int minus = roundTick - currentRoundTick;
        if (minus > 10)
        {
            timerLabel.Text = minus.ToString();
            timerLabel.Set("theme_override_colors/font_color", "#ffffff");
        }
        else
        {
            timerLabel.Text = minus.ToString();
            timerLabel.Set("theme_override_colors/font_color", "#eb2d5d");
        }
        // 都出牌了，就提前结束这回合进入战斗
        if (CheckPlayCard() == 3)
        {
            roundTimer.Stop();
            _ = StartBattle();
        }
        // 倒计时结束都没人出牌就随机出一张来比对
        if (currentRoundTick >= roundTick)
        {
            timerLabel.Visible = false;
            // 判断谁没出牌
            int checkNum = CheckPlayCard();
            if (checkNum == 0)
            {
                pausedLayer.PrintGameLogs($"玩家和敌人行动超时");
                var card1 = enemyHandManager.GetRandomCard();
                card1.ChangedToState(BaseState.State.Played);

                var card2 = playerHandManager.GetRandomCard();
                card2.ChangedToState(BaseState.State.Played);
            }
            else if (checkNum == 1)
            {
                pausedLayer.PrintGameLogs($"玩家行动超时");
                var card = playerHandManager.GetRandomCard();
                card.ChangedToState(BaseState.State.Played);
            }
            else if (checkNum == 2)
            {
                pausedLayer.PrintGameLogs($"敌人行动超时");
                var card = enemyHandManager.GetRandomCard();
                card.ChangedToState(BaseState.State.Played);
            }
            roundTimer.Stop();
            _ = StartBattle();
        }
    }

    private void GiveToCard(HandManager manager, OwnerType ownerType)
    {
        if (manager.HandFull()) return;
        var cardRes = cardTable.RandomPopOne();
        var card = ResourceLoader.Load<PackedScene>(cardScene).Instantiate() as Card;
        card.cardData = ResourceLoader.Load<CardData>(cardRes);
        card.ownerType = ownerType;
        card.disabled = true;
        manager.AddCard(card);
    }

    private void OnCardPlayed(Card card)
    {
        if (card.ownerType == OwnerType.Player)
        {
            pausedLayer.PrintGameLogs($"玩家出牌{card.cardData.Name}");
            _ = FollowTargetPostion(playerHandManager, rightCard, card);
        }
        else if (card.ownerType == OwnerType.Enemy)
        {
            pausedLayer.PrintGameLogs($"敌人出牌{card.cardData.Name}");
            _ = FollowTargetPostion(enemyHandManager, leftCard, card);
        }
    }

    private void OnDeathed(int ownerType)
    {
        var winer = ownerType == 1 ? "敌人" : "玩家";
        isGameOver = true;
        endLayer.SetInfo($"游戏结束，{winer}胜利！");
        endLayer.Visible = true;
    }

    private void InitDeckPanel()
    {
        startLayer.Visible = true;
        endLayer.Visible = false;
        leftCard.SelfModulate = leftCard.SelfModulate with { A = 0 };
        rightCard.SelfModulate = rightCard.SelfModulate with { A = 0 };
        _ = ShowTurnInfoAddBattlePanel(false);
    }

    private void InitCardTable()
    {
        cardTable.Clear();
        cardTable.Add("res://resources/card_data/qun/刘协.tres", 1);
        cardTable.Add("res://resources/card_data/qun/吕布.tres", 1);
        cardTable.Add("res://resources/card_data/qun/张角.tres", 1);
        cardTable.Add("res://resources/card_data/qun/文丑.tres", 1);
        cardTable.Add("res://resources/card_data/qun/袁绍.tres", 1);
        cardTable.Add("res://resources/card_data/qun/貂蝉.tres", 1);
        cardTable.Add("res://resources/card_data/qun/颜良.tres", 1);
        cardTable.Add("res://resources/card_data/shu/关羽.tres", 1);
        cardTable.Add("res://resources/card_data/shu/刘备.tres", 1);
        cardTable.Add("res://resources/card_data/shu/姜维.tres", 1);
        cardTable.Add("res://resources/card_data/shu/张飞.tres", 1);
        cardTable.Add("res://resources/card_data/shu/法正.tres", 1);
        cardTable.Add("res://resources/card_data/shu/诸葛亮.tres", 1);
        cardTable.Add("res://resources/card_data/shu/赵云.tres", 1);
        cardTable.Add("res://resources/card_data/shu/马超.tres", 1);
        cardTable.Add("res://resources/card_data/shu/黄忠.tres", 1);
        cardTable.Add("res://resources/card_data/wei/典韦.tres", 1);
        cardTable.Add("res://resources/card_data/wei/司马懿.tres", 1);
        cardTable.Add("res://resources/card_data/wei/夏侯渊.tres", 1);
        cardTable.Add("res://resources/card_data/wei/张辽.tres", 1);
        cardTable.Add("res://resources/card_data/wei/曹操.tres", 1);
        cardTable.Add("res://resources/card_data/wei/许褚.tres", 1);
        cardTable.Add("res://resources/card_data/wei/郭嘉.tres", 1);
        cardTable.Add("res://resources/card_data/wu/周泰.tres", 1);
        cardTable.Add("res://resources/card_data/wu/周瑜.tres", 1);
        cardTable.Add("res://resources/card_data/wu/太史慈.tres", 1);
        cardTable.Add("res://resources/card_data/wu/孙权.tres", 1);
        cardTable.Add("res://resources/card_data/wu/甘宁.tres", 1);
        cardTable.Add("res://resources/card_data/wu/陆逊.tres", 1);
        cardTable.Add("res://resources/card_data/wu/鲁肃.tres", 1);
    }

    // 检查谁没出牌，没出0，左边出了1，右边出了2，都出了3
    private int CheckPlayCard()
    {
        int leftPlayed = leftCard.GetChildCount() > 0 ? 1 : 0;
        int rightPlayed = rightCard.GetChildCount() > 0 ? 2 : 0;
        return leftPlayed + rightPlayed;
    }

    private async Task StartBattle()
    {
        // 显示回合信息
        await ShowTurnInfoAddBattlePanel(true);
        // 这里等待1秒，避免上个动画未结束
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        // 禁止播放动画时能交互，并且把卡牌状态还原
        playerHandManager.SetDisable(true);
        playerHandManager.ResetHandStatus();
        // 获取两个出牌区的卡牌信息，然后进行对决
        var cardEnemy = leftCard.GetChild(0) as Card;
        var cardPlayer = rightCard.GetChild(0) as Card;
        cardEnemy.SetBackShow(false);
        // 开始对决
        await BattleCard(cardEnemy, cardPlayer);
    }

    private async Task BattleCard(Card cardEnemy, Card cardPlayer)
    {
        // 先把卡牌的增量还原成0，然后再判断特殊的回合的延续BUFF
        cardEnemy.cardData.AdditionalPower = 0;
        cardEnemy.cardData.AdditionalWisdom = 0;
        cardPlayer.cardData.AdditionalPower = 0;
        cardPlayer.cardData.AdditionalWisdom = 0;
        if (buffs.Count > 0)
        {
            foreach (var bf in buffs)
            {
                if (bf.Target == OwnerType.Player)
                {
                    cardPlayer.cardData.AdditionalPower += bf.AdditionalPower;
                    cardPlayer.cardData.AdditionalWisdom += bf.AdditionalWisdom;
                }
                else if (bf.Target == OwnerType.Enemy)
                {
                    cardEnemy.cardData.AdditionalPower += bf.AdditionalPower;
                    cardEnemy.cardData.AdditionalWisdom += bf.AdditionalWisdom;
                }
            }
            buffs.Clear();
        }
        if (cardEnemy.cardData.Wisdom > cardPlayer.cardData.Wisdom)
        {
            // 播放攻击动画
            await cardEnemy.PlayAniAsync("攻击");
            await cardPlayer.PlayAniAsync("攻击");
        }
        else
        {
            // 播放攻击动画
            await cardPlayer.PlayAniAsync("攻击");
            await cardEnemy.PlayAniAsync("攻击");
        }
        // 比较卡牌
        var score = cardPlayer.cardData.Compare(cardEnemy.cardData);
        pausedLayer.PrintGameLogs("敌人卡牌信息：" + cardEnemy.cardData.ToString());
        pausedLayer.PrintGameLogs("玩家卡牌信息：" + cardPlayer.cardData.ToString());
        // 判断战后技能，否则胜者赢掉败者本轮牌，胜者本轮的牌弃牌。
        if (score == 0)
        {
            // 丢弃卡牌
            await FollowTargetPostion(leftCard, enemyCardPile, cardEnemy);
            await FollowTargetPostion(rightCard, playerCardPile, cardPlayer);
            // 触发伤害
            Task task1 = CastSkillAsync(skillIMPtScene, playerPos.GlobalPosition, enemyPos.GlobalPosition);
            Task task2 = CastSkillAsync(skillHMSScene, enemyPos.GlobalPosition, playerPos.GlobalPosition);
            await Task.WhenAll(task1, task2);
            pausedLayer.PrintGameLogs("双方战平，都受到1点伤害");
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Enemy, 1);
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Player, 1);
        }
        else if (score > 0)
        {
            // 战后技能判断
            if (cardPlayer.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardPlayer, true);
            }
            if (cardEnemy.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardEnemy, false);
            }
            // 丢弃卡牌和获取对方卡牌
            if (!cardEnemy.IsQueuedForDeletion() && cardEnemy.GetParent() == leftCard)
            {
                cardEnemy.ownerType = OwnerType.Player;
                await FollowTargetPostion(leftCard, playerHandManager, cardEnemy);
            }
            if (!cardPlayer.IsQueuedForDeletion() && cardPlayer.GetParent() == rightCard)
            {
                if (cardPlayer.cardData.Skill != "武圣")
                {
                    await FollowTargetPostion(rightCard, playerCardPile, cardPlayer);
                }
            }
            // 触发伤害
            await CastSkillAsync(skillIMPtScene, playerPos.GlobalPosition, enemyPos.GlobalPosition);
            pausedLayer.PrintGameLogs("玩家赢，敌人受到1点伤害");
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Enemy, 1);
        }
        else
        {
            // 战后技能判断
            if (cardEnemy.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardEnemy, true);
            }
            if (cardPlayer.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardPlayer, false);
            }
            // 丢弃卡牌和获取对方卡牌
            if (!cardPlayer.IsQueuedForDeletion() && cardPlayer.GetParent() == rightCard)
            {
                cardPlayer.ownerType = OwnerType.Enemy;
                await FollowTargetPostion(rightCard, enemyHandManager, cardPlayer);
            }
            if (!cardEnemy.IsQueuedForDeletion() && cardEnemy.GetParent() == leftCard)
            {
                if (cardEnemy.cardData.Skill != "武圣")
                {
                    await FollowTargetPostion(leftCard, enemyCardPile, cardEnemy);
                }
            }
            // 触发伤害
            await CastSkillAsync(skillHMSScene, enemyPos.GlobalPosition, playerPos.GlobalPosition);
            pausedLayer.PrintGameLogs("敌人赢，玩家受到一点伤害");
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Player, 1);
        }
        await ShowTurnInfoAddBattlePanel(false);
        // 临时处理，自动进入下回合
        if (!isGameOver)
        {
            NewRound();
        }
    }

    private async Task Aftermath(Card card, bool isWin)
    {
        // 处理战后技能的工作
        if (card.cardData.Skill == "举义" && !isWin)
        {
            // 直接进我方弃牌堆
            if (card.ownerType == OwnerType.Player)
            {
                await FollowTargetPostion(rightCard, playerCardPile, card);
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                await FollowTargetPostion(leftCard, enemyCardPile, card);
            }
        }
        if (card.cardData.Skill == "常胜" && isWin)
        {
            // 回到我方手牌
            if (card.ownerType == OwnerType.Player)
            {
                await FollowTargetPostion(rightCard, playerHandManager, card);
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                await FollowTargetPostion(leftCard, enemyHandManager, card);
            }
        }
        if (card.cardData.Skill == "武圣")
        {
            if (isWin)
            {
                // 我方跳过出牌
                if (card.ownerType == OwnerType.Player)
                {
                    pausedLayer.PrintGameLogs($"玩家出牌{card.cardData.Name}");
                    gameEvent.EmitSignal(GameEvent.SignalName.TurnSkipNext);
                }
                else if (card.ownerType == OwnerType.Enemy)
                {
                    pausedLayer.PrintGameLogs($"敌人出牌{card.cardData.Name}");
                }
            }
            else
            {
                // 从本局游戏移除卡牌
                card.QueueFree();
            }
        }
        if (card.cardData.Skill == "遗计" && !isWin)
        {
            // 给我方下个牌加Buff
            buffs.Add(new BuffInfo
            {
                Target = card.ownerType,
                AdditionalPower = 20
            });
        }
        if (card.cardData.Skill == "火神" && isWin)
        {
            // 随机丢弃对方一张手牌
            if (card.ownerType == OwnerType.Player)
            {
                var discardCard = enemyHandManager.GetRandomCard();
                if (discardCard != null)
                {
                    await AnalogTakeCardToPostion(enemyHandManager, enemyCardPile, discardCard);
                }
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                var discardCard = playerHandManager.GetRandomCard();
                if (discardCard != null)
                {
                    await AnalogTakeCardToPostion(playerHandManager, playerCardPile, discardCard);
                }
            }
        }
        if (card.cardData.Skill == "虎臣" && !isWin)
        {
            // 随机抽取一张我方弃牌堆里的吴国牌
            if (card.ownerType == OwnerType.Player)
            {
                var newCard = playerCardPile.GetRandomCard(CardData.CampType.Wu);
                if (newCard != null)
                {
                    await AnalogTakeCardToPostion(playerCardPile, playerHandManager, newCard);
                }
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                var newCard = enemyCardPile.GetRandomCard(CardData.CampType.Wu);
                if (newCard != null)
                {
                    await AnalogTakeCardToPostion(enemyCardPile, enemyHandManager, newCard);
                }
            }
        }
    }

    private async Task FollowTargetPostion(Control from, Control to, Card card)
    {
        card.ZIndex = 1;
        // 获取移动到的描点位置
        var targetPos = to.GlobalPosition;
        if (to is IDeck)
        {
            targetPos = (to as IDeck).GetMarkPosition();
        }
        // 移动卡牌到目标位置并修改父级
        var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "global_position", targetPos, moveDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            // 移除原来父级
            if (from is IDeck deckFrom)
            {
                deckFrom.RemoveCard(card);
            }
            else
            {
                from.RemoveChild(card);
            }
            // 添加到新的父级
            if (to is IDeck deckTo)
            {
                deckTo.AddCard(card);
            }
            else
            {
                to.AddChild(card);
            }
        }));
        await ToSignal(tween, "finished");
        card.ZIndex = 0;
    }

    private async Task AnalogTakeCardToPostion(Control from, Control to, Card card)
    {
        card.ZIndex = 1;
        // 先把卡向上提高30像素
        var targetPos = card.GlobalPosition with { Y = card.GlobalPosition.Y - 30 };
        // 获取移动到的描点位置
        var targetPos2 = to.GlobalPosition;
        if (to is IDeck)
        {
            targetPos = (to as IDeck).GetMarkPosition();
        }
        // 模拟抽卡的动作，然后移动到指定父级
        var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);     
        tween.TweenProperty(card, "global_position", targetPos, takeDuration);
        tween.Chain().TweenProperty(card, "global_position", targetPos2, moveDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            // 移除原来父级
            if (from is IDeck deckFrom)
            {
                deckFrom.RemoveCard(card);
            }
            else
            {
                from.RemoveChild(card);
            }
            // 添加到新的父级
            if (to is IDeck deckTo)
            {
                deckTo.AddCard(card);
            }
            else
            {
                to.AddChild(card);
            }
        }));
        await ToSignal(tween, "finished");
        card.ZIndex = 0;
    }

    private string NumberToChineseOptimized(int num)
    {
        if (num < 1 || num > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(num));
        }
        var sb = new StringBuilder();
        // 数字映射
        string[] digits = ["", "一", "二", "三", "四", "五", "六", "七", "八", "九"];
        if (num < 10)
        {
            sb.Append(digits[num]);
        }
        else if (num < 20)
        {
            sb.Append('十');
            if (num > 10)
            {
                sb.Append(digits[num % 10]);
            }
        }
        else
        {
            sb.Append(digits[num / 10]);
            sb.Append('十');
            if (num % 10 != 0)
            {
                sb.Append(digits[num % 10]);
            }
        }
        return sb.ToString();
    }

    private void HandOutCards()
    {
        if (cardTable.Count == 0) return;
        var tween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        for (int i = 0; i < startCardNum; i++)
        {
            tween.TweenCallback(Callable.From(() => {
                // 给敌人手牌
                GiveToCard(enemyHandManager, OwnerType.Enemy);
            }));
            tween.TweenCallback(Callable.From(() => {
                // 给玩家手牌
                GiveToCard(playerHandManager, OwnerType.Player);
            }));
            tween.TweenInterval(0.25f);
        }
        tween.Finished += NewRound;
    }

    private async Task ShowTurnInfoAddBattlePanel(bool isShow)
    {
        if(isShow)
        {
            var tween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tween.SetParallel(true);
            tween.TweenProperty(battleLayer, "self_modulate:a", 1, 1.0f);
            tween.TweenProperty(turnLayer, "modulate:a", 1, 1.0f);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        else
        {
            battleLayer.SelfModulate = battleLayer.SelfModulate with { A = 0 };
            turnLayer.Modulate = turnLayer.Modulate with { A = 0 };
        }
    }

    private void NewRound()
    {
        currentRound++;
        roundLabel.Text = $"第{NumberToChineseOptimized(currentRound)}局";
        currentRoundTick = 0;
        timerLabel.Visible = true;
        roundTimer.Start();
        gameEvent.EmitSignal(GameEvent.SignalName.TurnBegin);
        playerHandManager.SetDisable(false);
        pausedLayer.PrintGameLogs($"开始新的回合，当前回合数：{currentRound}");
    }

    private async Task CastSkillAsync(string skillScene, Vector2 startPos, Vector2 targerPos)
    {
        // 创建 TaskCompletionSource 来手动触发Task
        var tcs = new TaskCompletionSource();

        var skillNode = ResourceLoader.Load<PackedScene>(skillScene).Instantiate() as Node2D;
        skillLayer.AddChild(skillNode);
        skillNode.GlobalPosition = startPos;

        // 这里是不合适的做法，但是我这是卡牌回合制游戏，等待攻击动画完成不会影响游戏体验
        (skillNode as ISkillNode).ExecuteSkill(targerPos);
        (skillNode as ISkillNode).SkillPlayEnd += tcs.SetResult;

        // 等待委托执行完成
        await tcs.Task;
    }
}
