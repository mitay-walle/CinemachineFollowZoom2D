# CinemachineFollowZoom2D
Unity3d / Cinemachine add-on module for Cinemachine Virtual Camera that adjusts the Orthographic Size of the lens, depending on camera and target distance, or velocity

![](https://github.com/mitay-walle/CinemachineFollowZoom2D/blob/main/Inspector%20preview.png)
# Properties
- Pattern: choose what base value would be used for zooming by OrthographicSize
- - Camera to Target Distance
- - Camera to Target Velocity
- - Target Velocity
- Orthographic Size By Distance - AnimationCurve, describing changes to OrthographicSize by Component
- Damping - smoothig base value changes
- Gizmos - draw Wire Cube for final OrthographicSize and Wire Circle for input distance / velocity.magnitude at each keyframe in Orthographic Size By Distance - Curve
