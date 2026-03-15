using UnityEngine;

/// <summary>
/// Simple component attached to each spawned touch target.  The manager
/// assigns itself via <see cref="Initialize"/> so that the target can
/// notify it when it has been popped.  The TouchTarget itself does not
/// handle input directly – input is routed through the TouchPopManager
/// which performs a raycast and calls <see cref="TouchPopManager.HitTarget"/>
/// when appropriate.  This class is intentionally lightweight.
/// </summary>
public class TouchTarget : MonoBehaviour
{
    private TouchPopManager manager;

    /// <summary>
    /// Called by the spawner after instantiating the target so that
    /// this target knows which manager to notify when it is removed.
    /// </summary>
    /// <param name="manager">Reference to the <see cref="TouchPopManager"/> that spawned this target.</param>
    public void Initialize(TouchPopManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// Optional helper method that can be called by other scripts to
    /// immediately pop this target.  It simply delegates to the manager.
    /// </summary>
    public void Pop()
    {
        if (manager != null)
        {
            manager.HitTarget(this);
        }
    }
}