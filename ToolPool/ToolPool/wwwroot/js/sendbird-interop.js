/**
 * Sendbird JS SDK interop for Blazor WebAssembly.
 * Requires the Sendbird Chat UMD bundle loaded before this script.
 */
window.sendbirdInterop = (function () {
    let sb = null;
    let currentChannel = null;
    let handlerId = null;

    return {
        /**
         * Initialize the Sendbird SDK with the application ID.
         */
        init: function (appId) {
            if (typeof Sendbird === "undefined") {
                throw new Error("Sendbird CDN script not loaded. Check the <script> tag URL in App.razor.");
            }
            const { SendbirdChat, GroupChannelModule } = Sendbird;
            sb = SendbirdChat.init({
                appId: appId,
                modules: [new GroupChannelModule()]
            });
        },

        /**
         * Connect a user to Sendbird. Auto-creates the user if it doesn't exist.
         */
        connect: async function (userId) {
            if (!sb) throw new Error("SDK not initialized");
            await sb.connect(userId);
        },

        /**
         * Disconnect the current user.
         */
        disconnect: async function () {
            if (!sb) return;
            if (handlerId) {
                sb.groupChannel.removeGroupChannelHandler(handlerId);
                handlerId = null;
            }
            await sb.disconnect();
        },

        /**
         * Get a group channel by its URL and cache it.
         */
        getChannel: async function (channelUrl) {
            if (!sb) throw new Error("SDK not initialized");
            currentChannel = await sb.groupChannel.getChannel(channelUrl);
            return {
                channelUrl: currentChannel.url,
                name: currentChannel.name,
                memberCount: currentChannel.memberCount
            };
        },

        /**
         * Load previous messages from the current channel.
         * Returns an array of serializable message objects.
         */
        loadMessages: async function (channelUrl, limit) {
            if (!sb) throw new Error("SDK not initialized");
            const channel = await sb.groupChannel.getChannel(channelUrl);
            const params = {
                limit: limit || 50,
                reverse: false
            };
            const query = channel.createPreviousMessageListQuery(params);
            const messages = await query.load();
            return messages.map(function (m) {
                return {
                    messageId: m.messageId,
                    message: m.message,
                    senderId: m.sender ? m.sender.userId : "system",
                    senderNickname: m.sender ? m.sender.nickname : "System",
                    createdAt: m.createdAt
                };
            });
        },

        /**
         * Send a text message to the current channel.
         */
        sendMessage: async function (channelUrl, text) {
            if (!sb) throw new Error("SDK not initialized");
            const channel = await sb.groupChannel.getChannel(channelUrl);
            return new Promise(function (resolve, reject) {
                const params = { message: text };
                channel.sendUserMessage(params)
                    .onSucceeded(function (message) {
                        resolve({
                            messageId: message.messageId,
                            message: message.message,
                            senderId: message.sender ? message.sender.userId : "system",
                            senderNickname: message.sender ? message.sender.nickname : "System",
                            createdAt: message.createdAt
                        });
                    })
                    .onFailed(function (err) {
                        reject(err);
                    });
            });
        },

        /**
         * Register a channel handler that calls back into Blazor
         * when a new message is received.
         * @param {DotNetObjectReference} dotnetHelper
         */
        registerHandler: function (channelUrl, dotnetHelper) {
            if (!sb) throw new Error("SDK not initialized");
            const { GroupChannelHandler } = Sendbird;

            handlerId = "blazor_handler_" + Date.now();
            const handler = new GroupChannelHandler({
                onMessageReceived: function (channel, message) {
                    if (channel.url !== channelUrl) return;
                    var msg = {
                        messageId: message.messageId,
                        message: message.message,
                        senderId: message.sender ? message.sender.userId : "system",
                        senderNickname: message.sender ? message.sender.nickname : "System",
                        createdAt: message.createdAt
                    };
                    dotnetHelper.invokeMethodAsync("OnMessageReceived", JSON.stringify(msg));
                }
            });
            sb.groupChannel.addGroupChannelHandler(handlerId, handler);
        }
    };
})();
