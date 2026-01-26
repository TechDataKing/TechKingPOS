using TechKingPOS.App.Security;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;


namespace TechKingPOS.App.Services
{
    public class BranchContextGuard
{
    private int _originalBranchId;
    private bool _isGuardActive;

    public BranchContextGuard()
    {
        _originalBranchId = SessionContext.CurrentBranchId;
        _isGuardActive = false;
    }

    // Activate the guard when opening Reports window
    public void ActivateReportsWindow()
    {
        if (!_isGuardActive)
        {
            // Save the current branch context and set it to "all branches" (e.g., -1)
            _originalBranchId = SessionContext.CurrentBranchId;
            SessionContext.CurrentBranchId = 0;  // "All branches"
            _isGuardActive = true;
        }
    }

    // Deactivate the guard and restore the branch context when Reports window closes
    public void DeactivateReportsWindow()
    {
        if (_isGuardActive)
        {
            // Restore the original branch context
            SessionContext.CurrentBranchId = _originalBranchId;
            _isGuardActive = false;
        }
    }

    // Check if the guard is active
    public bool IsReportsWindowActive()
    {
        return _isGuardActive;
    }
}
}
