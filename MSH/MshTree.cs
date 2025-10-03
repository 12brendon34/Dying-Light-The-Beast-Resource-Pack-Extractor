namespace MSH;

public struct MshTree
{
    public MshNode Node;
    public ICollTree CollGeom;
    public ICollTree CollHull;
    public MtoolFormat[] Mesh;
    public uint Index;
}