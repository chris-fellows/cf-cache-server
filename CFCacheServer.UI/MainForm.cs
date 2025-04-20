using CFConnectionMessaging.Models;
using CFCacheServer.Interfaces;
using CFCacheServer.Models;
using CFCacheServer.Services;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CFCacheServer.UI
{
    public partial class MainForm : Form
    {
        private ICacheServerClient? _cacheServerClient;

        private enum MyTreeNodeTypes
        {
            Environments,
            Environment,
            Key,
            Unknown
        }

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Returns node type
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private MyTreeNodeTypes GetNodeType(TreeNode node)
        {
            if (node.Name.StartsWith("Environments")) return MyTreeNodeTypes.Environments;
            if (node.Name.StartsWith("Environment.")) return MyTreeNodeTypes.Environment;
            if (node.Name.StartsWith("Key.")) return MyTreeNodeTypes.Key;

            return MyTreeNodeTypes.Unknown;
        }

        public MainForm(EndpointInfo endpointInfo, string securityKey)
        {
            InitializeComponent();

            _cacheServerClient = new CacheServerClient(endpointInfo, 10139, securityKey);

            Task.Run(() => DisplayCacheAsync());
        }

        private async Task DisplayCacheAsync()
        {
            tvwTree.Nodes.Clear();

            var nodeEnvironments = tvwTree.Nodes.Add("Environments", "Environments");

            // TODO: Get environment list from cache server
            var environments = new[] { "Default" };

            foreach (var environment in environments)
            {
                _cacheServerClient.Environment = environment;

                var nodeEnvironment = nodeEnvironments.Nodes.Add($"Environment.{environment}", environment);

                var keys = await _cacheServerClient.GetKeysByFilterAsync(new CacheItemFilter());

                this.Invoke((Action)delegate
                {
                    foreach (var key in keys)
                    {
                        var nodeKey = nodeEnvironment.Nodes.Add($"Key.{key}", key);
                        nodeKey.Tag = key;
                    }
                });
            }
        }

        private void tvwTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var nodeType = GetNodeType(e.Node);

            if (nodeType == MyTreeNodeTypes.Key)
            {
                var key = (string)e.Node.Tag;

                var cacheItem = _cacheServerClient.GetByKeyAsync<object>(key).Result;
                int xxx = 1000;
            }                 
        }
    }
}
