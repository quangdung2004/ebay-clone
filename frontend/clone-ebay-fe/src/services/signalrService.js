import * as signalR from '@microsoft/signalr';

const HUB_URL = import.meta.env.VITE_HUB_URL || '/hubs/auction';

class SignalRService {
    constructor() {
        this.connection = null;
    }

    async connect() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) return;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                accessTokenFactory: () => localStorage.getItem('accessToken') || '',
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .build();

        try {
            await this.connection.start();
            console.log('SignalR Connected to auction hub');
        } catch (err) {
            console.error('SignalR Connection Error: ', err);
        }
    }

    async joinProductRoom(productId) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            await this.connect();
        }
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke('JoinProductRoom', Number(productId));
            } catch (err) {
                console.error('Error joining product room:', err);
            }
        }
    }

    async leaveProductRoom(productId) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }
        try {
            await this.connection.invoke('LeaveProductRoom', Number(productId));
        } catch (err) {
            console.error('Error leaving product room:', err);
        }
    }

    onBidPlaced(callback) {
        if (!this.connection) return;
        this.connection.on('BidPlaced', callback);
    }

    offBidPlaced(callback) {
        if (!this.connection) return;
        this.connection.off('BidPlaced', callback);
    }

    onAuctionClosed(callback) {
        if (!this.connection) return;
        this.connection.on('AuctionClosed', callback);
    }

    offAuctionClosed(callback) {
        if (!this.connection) return;
        this.connection.off('AuctionClosed', callback);
    }

    async disconnect() {
        if (this.connection) {
            this.connection.off('BidPlaced');
            this.connection.off('AuctionClosed');
            await this.connection.stop();
            this.connection = null;
        }
    }
}

export default new SignalRService();
