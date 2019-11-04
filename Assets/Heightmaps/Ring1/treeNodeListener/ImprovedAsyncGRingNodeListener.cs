using System.Threading.Tasks;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public abstract class ImprovedAsyncGRingNodeListener : IAsyncGRingNodeListener
    {
        public abstract Task ShowNodeAsync();
        public abstract Task UpdateNodeAsync();
        public abstract Task HideNodeAsync();

        public abstract Task CreatedNewNodeAsync();
        public abstract Task Destroy();

        private bool _wasLastTimeVisible = false;

        public async Task UpdateAsync()
        {
            if (!_wasLastTimeVisible)
            {
                await ShowNodeAsync();
                _wasLastTimeVisible = true;
            }
            await UpdateNodeAsync();
        }

        public async Task DoNotDisplayAsync()
        {
            _wasLastTimeVisible = false;
            await HideNodeAsync();
        }
    }
}