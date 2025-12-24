using System.Collections.Generic;

namespace PSMB.Physics.Classes
{
    public class ManifoldPool
    {
        private readonly Stack<Manifold> pool = new();

        public Manifold Get()
        {
            if(pool.Count <= 0)
            {
                return new Manifold();
            }

            var m = pool.Pop();
            m.Reset();
            return m;
        }

        public void Return(Manifold m)
        {
            m.Reset();
            pool.Push(m);
        }
    }
}
