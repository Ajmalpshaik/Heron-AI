// NOT STANDALONE. Assumes `elements`, `flipHand` and `flipFacing` are in scope;
// leaves `flipped`, `cannotFlip` and `notFamilyInstance` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A FLIP IS NOT A ROTATION, and reaching for rotation instead is the mistake
// this fragment exists to prevent. A flip uses the family's OWN control, so it
// respects how the family was authored and keeps a hosted element correct
// against its host. Rotating a door 180 degrees moves geometry and can leave it
// in the wall the wrong way round, which looks right in plan and is wrong on
// site.
//
// ONLY A FAMILY INSTANCE CAN FLIP AT ALL, and not every family instance can
// flip in a given direction - whether a flip control exists is a decision the
// family AUTHOR made. So both are asked before either is attempted:
//
//   notFamilyInstance   a wall, a duct, a system element. Nothing to flip
//   cannotFlip          a family instance whose author gave it no such control
//
// Kept apart because they need different answers. The first is the wrong
// selection; the second is a family that would have to be edited.
//
// ASKING FIRST RATHER THAN CATCHING. `CanFlipHand` and `CanFlipFacing` are
// cheap and exact, so an unsupported flip is a reported outcome rather than an
// exception - and a batch of forty does not stop at the eleventh.

var flipped = 0;
var cannotFlip = new List<ElementId>();
var notFamilyInstance = new List<ElementId>();

foreach (var element in elements)
{
    var instance = element as FamilyInstance;
    if (instance == null)
    {
        if (element != null) notFamilyInstance.Add(element.Id);
        continue;
    }

    var did = false;

    if (flipHand)
    {
        if (instance.CanFlipHand) { instance.flipHand(); did = true; }
    }

    if (flipFacing)
    {
        if (instance.CanFlipFacing) { instance.flipFacing(); did = true; }
    }

    if (did) flipped++;
    else cannotFlip.Add(instance.Id);
}
