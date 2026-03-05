using Godot;
using System;
using System.Reflection.Metadata;

public partial class Main : Node
{
    private Button startBtn;
    private Button gameInfoBtn;
    private Button quitBtn;
    private PopupPanel popupPanel;
    private RichTextLabel richText;

    private string transitionScene = "res://ui/transition.tscn";
    private string gameInfoPath = "res://resources/others/game_info.txt";

    public override void _Ready()
	{
        startBtn = GetNode<Button>("%开始游戏");
        gameInfoBtn = GetNode<Button>("%游戏说明");
        quitBtn = GetNode<Button>("%退出游戏");
        popupPanel = GetNode<PopupPanel>("%游戏说明弹窗");
        richText = GetNode<RichTextLabel>("%文本标签");

        startBtn.Pressed += StartBtn_Pressed;
        gameInfoBtn.Pressed += GameInfoBtn_Pressed;
        quitBtn.Pressed += QuitBtn_Pressed;

        LoadGameInfo();
    }

    private async void StartBtn_Pressed()
    {
        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.WipeInAsync();

        GetTree().ChangeSceneToFile("res://ui/game_panel.tscn");

        await tranNode.WipeOutAsync();

        tranNode.QueueFree();
    }

    private void GameInfoBtn_Pressed()
    {
        popupPanel.Show();
    }

    private void QuitBtn_Pressed()
    {
        GetTree().Quit();
    }

    private void LoadGameInfo()
    {
        if (!FileAccess.FileExists(gameInfoPath))
        {
            return;
        }

        using var file = FileAccess.Open(gameInfoPath, FileAccess.ModeFlags.Read);
        var gameInfoStr = file.GetAsText();

        richText.Text = gameInfoStr;
    }
}
