using Godot;
using System;

public partial class GameOverScene : Control
{
    private Label endLabel;
    private Button restartBtn;
    private Button exitBtn;

    private string transitionScene = "res://ui/transition.tscn";

    public override void _Ready()
    {
        endLabel = GetNode<Label>("%提示信息");
        restartBtn = GetNode<Button>("%重新开始");
        exitBtn = GetNode<Button>("%结束游戏");

        restartBtn.Pressed += StartGame;
        exitBtn.Pressed += QuitGame;
    }

    public void SetInfo(string text)
    {
        endLabel.Text = text;
    }

    private async void StartGame()
    {
        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.FadeInAsync();

        GetTree().ChangeSceneToFile("res://ui/game_panel.tscn");

        await tranNode.FadeOutAsync();

        tranNode.QueueFree();
    }

    private void QuitGame()
    {
        GetTree().Quit();
    }
}
