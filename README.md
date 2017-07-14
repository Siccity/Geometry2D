### Geometry2D
A set of 2D Geometric helper components and structs for Unity3D.
All structs are immutable.
Useful for many things including calculating intersections, projections, distances and lerping.

**Structs**
* *Line2D* - A line between point *a* and point *b*.
* *Triangle2D* - A 3-gon defined by point *a*, *b* and *c*. Can be converted into an array of *Line2D*.
* *Polygon2D* - An n-gon defined by an array of points. Can be converted into an array of *Triangle2D* or *Line2D*.
* *Bounds2D* - A simple axis-alligned bounding box for fast collision checks.

**Components**
* *Area2D* - Lets you assign and edit a polygon in 3D-space for various purposes.

**Extensions**
* *Geometry2D* - Contains extension methods for Vector2.

![alt text](http://i.imgur.com/OWEFj72.gif "Area2D demonstration")
