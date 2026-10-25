// Pure presentation/restart gate, independent of scene loading and UI callbacks.
public sealed class RoundEndState
{
    public bool Visible { get; private set; }
    public bool IsReloading { get; private set; }

    public bool Observe(HuntRules.Phase phase)
    {
        if (Visible || phase != HuntRules.Phase.RoundOver) return false;
        Visible = true;
        return true;
    }

    public bool TryBeginReload(bool roundOver, bool registeredScene)
    {
        if (!Visible || IsReloading || !roundOver || !registeredScene) return false;
        IsReloading = true;
        return true;
    }

    public void ReloadFailed() { IsReloading = false; }

    public static string ResultText(int winner)
    {
        if (winner < 0) return "DRAW\nNO SURVIVORS";
        return "P" + (winner + 1) + " WINS!\nLAST PLAYER ALIVE - " + HuntPresentation.ColorName(winner);
    }
}
