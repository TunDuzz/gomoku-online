// SignalR Client for Gomoku Online
class SignalRClient {
    constructor() {
        this.gameHub = null;
        this.roomHub = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 2000;
    }

    // Initialize SignalR connections
    async initialize() {
        try {
            // Initialize GameHub
            this.gameHub = new signalR.HubConnectionBuilder()
                .withUrl("/gameHub")
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .build();

            // Initialize RoomHub
            this.roomHub = new signalR.HubConnectionBuilder()
                .withUrl("/roomHub")
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .build();

            // Set up event handlers
            this.setupGameHubHandlers();
            this.setupRoomHubHandlers();

            // Start connections
            await this.startConnections();

            console.log("SignalR connections established");
        } catch (error) {
            console.error("Failed to initialize SignalR:", error);
        }
    }

    // Start both hub connections
    async startConnections() {
        try {
            await Promise.all([
                this.gameHub.start(),
                this.roomHub.start()
            ]);
            this.isConnected = true;
            this.reconnectAttempts = 0;
        } catch (error) {
            console.error("Failed to start SignalR connections:", error);
            this.handleReconnect();
        }
    }

    // Handle reconnection
    handleReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`Attempting to reconnect... (${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
            
            setTimeout(() => {
                this.startConnections();
            }, this.reconnectDelay * this.reconnectAttempts);
        } else {
            console.error("Max reconnection attempts reached");
            this.showConnectionError();
        }
    }

    // Show connection error to user
    showConnectionError() {
        const toast = document.createElement('div');
        toast.className = 'alert alert-danger position-fixed';
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.innerHTML = `
            <i class="fas fa-exclamation-triangle me-2"></i>
            <strong>Kết nối bị mất!</strong>
            <br>
            <small>Vui lòng tải lại trang để kết nối lại.</small>
            <button type="button" class="btn-close ms-2" onclick="this.parentElement.remove()"></button>
        `;
        document.body.appendChild(toast);
    }

    // Setup GameHub event handlers
    setupGameHubHandlers() {
        // Connection events
        this.gameHub.onreconnecting(() => {
            console.log("GameHub reconnecting...");
            this.showReconnectingMessage();
        });

        this.gameHub.onreconnected(() => {
            console.log("GameHub reconnected");
            this.hideReconnectingMessage();
        });

        this.gameHub.onclose(() => {
            console.log("GameHub connection closed");
            this.isConnected = false;
        });

        // Game events
        this.gameHub.on("MoveMade", (data) => {
            this.handleMoveMade(data);
        });

        this.gameHub.on("UserJoinedGame", (userId, gameId) => {
            this.handleUserJoinedGame(userId, gameId);
        });

        this.gameHub.on("UserLeftGame", (userId, gameId) => {
            this.handleUserLeftGame(userId, gameId);
        });

        this.gameHub.on("DrawRequested", (userId, gameId) => {
            this.handleDrawRequested(userId, gameId);
        });

        this.gameHub.on("DrawResponse", (userId, accepted) => {
            this.handleDrawResponse(userId, accepted);
        });

        this.gameHub.on("GameEnded", (data) => {
            this.handleGameEnded(data);
        });

        this.gameHub.on("MessageReceived", (data) => {
            this.handleGameMessageReceived(data);
        });

        this.gameHub.on("Error", (message) => {
            this.showError(message);
        });
    }

    // Setup RoomHub event handlers
    setupRoomHubHandlers() {
        // Connection events
        this.roomHub.onreconnecting(() => {
            console.log("RoomHub reconnecting...");
        });

        this.roomHub.onreconnected(() => {
            console.log("RoomHub reconnected");
        });

        this.roomHub.onclose(() => {
            console.log("RoomHub connection closed");
        });

        // Room events
        this.roomHub.on("UserJoinedRoom", (userId, roomId) => {
            this.handleUserJoinedRoom(userId, roomId);
        });

        this.roomHub.on("UserLeftRoom", (userId, roomId) => {
            this.handleUserLeftRoom(userId, roomId);
        });

        this.roomHub.on("RoomMessageReceived", (data) => {
            this.handleRoomMessageReceived(data);
        });

        this.roomHub.on("PlayerReadyStatusChanged", (data) => {
            this.handlePlayerReadyStatusChanged(data);
        });

        this.roomHub.on("GameStarting", (data) => {
            this.handleGameStarting(data);
        });

        this.roomHub.on("RoomStatusChanged", (data) => {
            this.handleRoomStatusChanged(data);
        });

        this.roomHub.on("PlayerJoined", (data) => {
            this.handlePlayerJoined(data);
        });

        this.roomHub.on("PlayerLeft", (data) => {
            this.handlePlayerLeft(data);
        });

        this.roomHub.on("UserTyping", (data) => {
            this.handleUserTyping(data);
        });

        this.roomHub.on("Pong", (timestamp) => {
            console.log("Pong received:", timestamp);
        });

        this.roomHub.on("Error", (message) => {
            this.showError(message);
        });
    }

    // GameHub methods
    async joinGame(gameId) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("JoinGame", gameId);
        } catch (error) {
            console.error("Failed to join game:", error);
        }
    }

    async leaveGame(gameId) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("LeaveGame", gameId);
        } catch (error) {
            console.error("Failed to leave game:", error);
        }
    }

    async makeMove(gameId, row, column) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("MakeMove", gameId, row, column);
        } catch (error) {
            console.error("Failed to make move:", error);
        }
    }

    async requestDraw(gameId) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("RequestDraw", gameId);
        } catch (error) {
            console.error("Failed to request draw:", error);
        }
    }

    async respondToDraw(gameId, accept) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("RespondToDraw", gameId, accept);
        } catch (error) {
            console.error("Failed to respond to draw:", error);
        }
    }

    async surrender(gameId) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("Surrender", gameId);
        } catch (error) {
            console.error("Failed to surrender:", error);
        }
    }

    async sendGameMessage(gameId, message) {
        if (!this.isConnected) return;
        try {
            await this.gameHub.invoke("SendMessage", gameId, message);
        } catch (error) {
            console.error("Failed to send game message:", error);
        }
    }

    // RoomHub methods
    async joinRoom(roomId) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("JoinRoom", roomId);
        } catch (error) {
            console.error("Failed to join room:", error);
        }
    }

    async leaveRoom(roomId) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("LeaveRoom", roomId);
        } catch (error) {
            console.error("Failed to leave room:", error);
        }
    }

    async sendRoomMessage(roomId, message) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("SendRoomMessage", roomId, message);
        } catch (error) {
            console.error("Failed to send room message:", error);
        }
    }

    async setReadyStatus(roomId, isReady) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("SetReadyStatus", roomId, isReady);
        } catch (error) {
            console.error("Failed to set ready status:", error);
        }
    }

    async startGame(roomId) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("StartGame", roomId);
        } catch (error) {
            console.error("Failed to start game:", error);
        }
    }

    async startTyping(roomId) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("StartTyping", roomId);
        } catch (error) {
            console.error("Failed to start typing:", error);
        }
    }

    async stopTyping(roomId) {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("StopTyping", roomId);
        } catch (error) {
            console.error("Failed to stop typing:", error);
        }
    }

    async ping() {
        if (!this.isConnected) return;
        try {
            await this.roomHub.invoke("Ping");
        } catch (error) {
            console.error("Failed to ping:", error);
        }
    }

    // Event handlers
    handleMoveMade(data) {
        console.log("Move made:", data);
        // Update game board
        if (window.gameBoard) {
            window.gameBoard.updateMove(data.row, data.column, data.symbol);
        }
        
        // Show notification
        this.showNotification(`Nước đi: (${data.row}, ${data.column})`, "info");
        
        // Handle game end
        if (data.isWin) {
            this.showNotification("Game kết thúc!", "success");
            setTimeout(() => {
                location.reload();
            }, 2000);
        }
    }

    handleUserJoinedGame(userId, gameId) {
        console.log("User joined game:", userId, gameId);
        this.showNotification("Người chơi đã tham gia", "info");
    }

    handleUserLeftGame(userId, gameId) {
        console.log("User left game:", userId, gameId);
        this.showNotification("Người chơi đã rời đi", "warning");
    }

    handleDrawRequested(userId, gameId) {
        console.log("Draw requested:", userId, gameId);
        const accept = confirm("Đối thủ xin hòa. Bạn có đồng ý không?");
        this.respondToDraw(gameId, accept);
    }

    handleDrawResponse(userId, accepted) {
        console.log("Draw response:", userId, accepted);
        if (accepted) {
            this.showNotification("Hòa!", "info");
        } else {
            this.showNotification("Từ chối hòa", "warning");
        }
    }

    handleGameEnded(data) {
        console.log("Game ended:", data);
        this.showNotification("Game kết thúc!", "success");
        setTimeout(() => {
            location.reload();
        }, 2000);
    }

    handleGameMessageReceived(data) {
        console.log("Game message received:", data);
        // Add message to chat
        this.addChatMessage(data.userId, data.message, data.timestamp);
    }

    handleUserJoinedRoom(userId, roomId) {
        console.log("User joined room:", userId, roomId);
        this.showNotification("Người chơi đã tham gia phòng", "info");
    }

    handleUserLeftRoom(userId, roomId) {
        console.log("User left room:", userId, roomId);
        this.showNotification("Người chơi đã rời phòng", "warning");
    }

    handleRoomMessageReceived(data) {
        console.log("Room message received:", data);
        // Add message to room chat
        this.addRoomChatMessage(data.userId, data.message, data.timestamp);
    }

    handlePlayerReadyStatusChanged(data) {
        console.log("Player ready status changed:", data);
        // Update player ready status in UI
        this.updatePlayerReadyStatus(data.userId, data.isReady);
    }

    handleGameStarting(data) {
        console.log("Game starting:", data);
        this.showNotification("Game đang bắt đầu...", "success");
        setTimeout(() => {
            location.reload();
        }, 2000);
    }

    handleRoomStatusChanged(data) {
        console.log("Room status changed:", data);
        // Update room status in UI
        this.updateRoomStatus(data.status);
    }

    handlePlayerJoined(data) {
        console.log("Player joined:", data);
        this.showNotification(`${data.playerName} đã tham gia`, "info");
    }

    handlePlayerLeft(data) {
        console.log("Player left:", data);
        this.showNotification(`${data.playerName} đã rời đi`, "warning");
    }

    handleUserTyping(data) {
        console.log("User typing:", data);
        // Show typing indicator
        this.showTypingIndicator(data.userId, data.isTyping);
    }

    // UI helper methods
    showNotification(message, type = "info") {
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(toast);
        
        // Auto remove after 5 seconds
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, 5000);
    }

    showError(message) {
        this.showNotification(message, "danger");
    }

    showReconnectingMessage() {
        const toast = document.createElement('div');
        toast.id = 'reconnecting-toast';
        toast.className = 'alert alert-warning position-fixed';
        toast.style.cssText = 'top: 20px; left: 50%; transform: translateX(-50%); z-index: 9999;';
        toast.innerHTML = `
            <i class="fas fa-sync fa-spin me-2"></i>
            Đang kết nối lại...
        `;
        document.body.appendChild(toast);
    }

    hideReconnectingMessage() {
        const toast = document.getElementById('reconnecting-toast');
        if (toast) {
            toast.remove();
        }
    }

    addChatMessage(userId, message, timestamp) {
        // Implementation depends on your chat UI
        console.log("Add chat message:", { userId, message, timestamp });
    }

    addRoomChatMessage(userId, message, timestamp) {
        // Implementation depends on your room chat UI
        console.log("Add room chat message:", { userId, message, timestamp });
    }

    updatePlayerReadyStatus(userId, isReady) {
        // Implementation depends on your player list UI
        console.log("Update player ready status:", { userId, isReady });
    }

    updateRoomStatus(status) {
        // Implementation depends on your room status UI
        console.log("Update room status:", status);
    }

    showTypingIndicator(userId, isTyping) {
        // Implementation depends on your typing indicator UI
        console.log("Show typing indicator:", { userId, isTyping });
    }
}

// Global SignalR client instance
window.signalRClient = new SignalRClient();

// Initialize when page loads
document.addEventListener('DOMContentLoaded', () => {
    window.signalRClient.initialize();
});
