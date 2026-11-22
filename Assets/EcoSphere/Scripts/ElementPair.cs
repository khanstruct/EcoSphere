using System;
namespace EcoSphere
{
    // Recipe order is irrelevant, including self-combinations.
    public readonly struct ElementPair : IEquatable<ElementPair>
    {
        public ElementId First {get;}
        public ElementId Second {get;}
        public ElementPair(ElementId first,ElementId second)
        {
            if((int)first<=(int)second){First=first;Second=second;}else{First=second;Second=first;}
        }
        public bool Equals(ElementPair other)=>First==other.First && Second==other.Second;
        public override bool Equals(object obj)=>obj is ElementPair other && Equals(other);
        public override int GetHashCode(){unchecked{return (int)First*397^(int)Second;}}
        public override string ToString()=>First+" + "+Second;
    }
}
