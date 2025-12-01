import os
import time
import requests
import logging
from requests.auth import HTTPBasicAuth
from datetime import datetime

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s')

RABBIT_HOST = os.environ.get('RABBIT_HOST', 'localhost')
RABBIT_PORT = os.environ.get('RABBIT_PORT', '15672')
RABBIT_USER = os.environ.get('RABBIT_USER', 'guest')
RABBIT_PASS = os.environ.get('RABBIT_PASS', 'guest')
CHAOS_HOST = os.environ.get('CHAOS_HOST', 'localhost')
CHAOS_PORT = os.environ.get('CHAOS_PORT', '8082')
PRODUCER_HOST = os.environ.get('PRODUCER_HOST', 'localhost')
PRODUCER_PORT = os.environ.get('PRODUCER_PORT', '8080')
CONSUMER_HOST = os.environ.get('CONSUMER_HOST', 'localhost')
CONSUMER_PORT = os.environ.get('CONSUMER_PORT', '8081')
MONITOR_INTERVAL = float(os.environ.get('MONITOR_INTERVAL', '1'))

BASE_URL = f"http://{RABBIT_HOST}:{RABBIT_PORT}/api"
CHAOS_URL = f"http://{CHAOS_HOST}:{CHAOS_PORT}"
PRODUCER_URL = f"http://{PRODUCER_HOST}:{PRODUCER_PORT}"
CONSUMER_URL = f"http://{CONSUMER_HOST}:{CONSUMER_PORT}"


def get_auth():
    return HTTPBasicAuth(RABBIT_USER, RABBIT_PASS)


def check_chaos_monkey_health():
    """Check if chaos monkey is running"""
    try:
        resp = requests.get(f"{CHAOS_URL}/health", timeout=5)
        return resp.status_code == 200
    except Exception:
        return False


def get_chaos_stats():
    """Get chaos monkey statistics"""
    try:
        resp = requests.get(f"{CHAOS_URL}/stats", timeout=5)
        resp.raise_for_status()
        return resp.json()
    except Exception:
        return None


def get_dotnet_health(service_url, service_name):
    """Get health status from dotnet service"""
    try:
        resp = requests.get(f"{service_url}/health/ready", timeout=5)
        return resp.json()
    except Exception:
        return None


def get_rabbitmq_stats():
    """Get RabbitMQ server statistics"""
    try:
        resp = requests.get(f"{BASE_URL}/overview", auth=get_auth(), timeout=5)
        resp.raise_for_status()
        data = resp.json()

        queues = get_queues()
        total_messages = sum(q.get('messages', 0) for q in queues)
        messages_ready = sum(q.get('messages_ready', 0) for q in queues)
        messages_unacked = sum(q.get('messages_unacknowledged', 0)
                               for q in queues)

        return {
            'status': 'up',
            'connections': data.get('object_totals', {}).get('connections', 0),
            'channels': data.get('object_totals', {}).get('channels', 0),
            'queues': data.get('object_totals', {}).get('queues', 0),
            'messages': total_messages,
            'messages_ready': messages_ready,
            'messages_unacked': messages_unacked,
        }
    except Exception:
        return {'status': 'down'}


def get_connections():
    """Get all active connections"""
    try:
        resp = requests.get(f"{BASE_URL}/connections",
                            auth=get_auth(), timeout=5)
        resp.raise_for_status()
        return resp.json()
    except Exception:
        return []


def get_channels():
    """Get all active channels"""
    try:
        resp = requests.get(f"{BASE_URL}/channels", auth=get_auth(), timeout=5)
        resp.raise_for_status()
        return resp.json()
    except Exception:
        return []


def get_queues():
    """Get all queues"""
    try:
        resp = requests.get(f"{BASE_URL}/queues", auth=get_auth(), timeout=5)
        resp.raise_for_status()
        return resp.json()
    except Exception:
        return []


def print_dotnet_health(health_data, service_name):
    """Print dotnet service health information"""
    if not health_data:
        print(f"\n[{service_name}] Status: ✗ DOWN")
        return

    status = health_data.get('status', 'Unknown')
    status_symbol = '✓' if status == 'Healthy' else '✗'
    print(f"\n[{service_name}] Status: {status_symbol} {status}")

    entries = health_data.get('entries', {})
    for entry_name, entry_data in entries.items():
        entry_status = entry_data.get('status', 'Unknown')
        entry_symbol = '✓' if entry_status == 'Healthy' else '✗'
        print(f"  - {entry_name}: {entry_symbol} {entry_status}")

        endpoints = entry_data.get('data', {}).get('Endpoints', {})
        for endpoint_name, endpoint_data in endpoints.items():
            endpoint_status = endpoint_data.get('status', 'Unknown')
            endpoint_symbol = '✓' if endpoint_status == 'Healthy' else '✗'
            description = endpoint_data.get('description', '')
            print(f"    • {endpoint_name}: {endpoint_symbol} {description}")


def print_report(stats, connections, channels, queues, chaos_healthy, chaos_stats, producer_health, consumer_health):
    """Print formatted monitoring report"""
    os.system('clear' if os.name == 'posix' else 'cls')

    print("\n" + "="*80)
    print(
        f"MONITORING REPORT - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("="*80)

    print(
        f"\n[CHAOS MONKEY] Status: {'✓ HEALTHY' if chaos_healthy else '✗ DOWN'}")

    if chaos_stats:
        print(f"  Total Executions: {chaos_stats.get('total_executions', 0)}")
        print(
            f"  Kill Connection: {chaos_stats['kill_connection']['count']} (errors: {chaos_stats['kill_connection']['errors']})")
        print(
            f"  Kill Channel: {chaos_stats['kill_channel']['count']} (errors: {chaos_stats['kill_channel']['errors']})")
        print(
            f"  Introduce Delay: {chaos_stats['introduce_delay']['count']} (errors: {chaos_stats['introduce_delay']['errors']})")

        if chaos_stats.get('last_execution'):
            last_exec = chaos_stats['last_execution']
            print(
                f"  Last Execution: {last_exec['strategy']} at {last_exec['timestamp']} ({last_exec['status']})")

        if chaos_stats.get('last_error'):
            last_err = chaos_stats['last_error']
            print(
                f"  Last Error: {last_err['strategy']} - {last_err['error']}")

    print_dotnet_health(producer_health, "PRODUCER")
    print_dotnet_health(consumer_health, "CONSUMER")

    if stats['status'] == 'up':
        print(f"\n[RABBITMQ SERVER] Status: ✓ UP")
        print(f"  Connections: {stats['connections']}")
        print(f"  Channels: {stats['channels']}")
        print(f"  Queues: {stats['queues']}")
        print(f"  Total Messages: {stats['messages']}")
        print(f"  Messages Ready: {stats['messages_ready']}")
        print(f"  Messages Unacknowledged: {stats['messages_unacked']}")
    else:
        print(f"\n[RABBITMQ SERVER] Status: ✗ DOWN")
        return

    print(f"\n[CONNECTIONS] Total: {len(connections)}")
    for conn in connections[:5]:  # Show first 5
        print(f"  - {conn['name']} (from {conn.get('peer_host', 'unknown')})")
    if len(connections) > 5:
        print(f"  ... and {len(connections) - 5} more")

    print(f"\n[CHANNELS] Total: {len(channels)}")
    for channel in channels[:5]:
        print(
            f"  - {channel['name']} (state: {channel.get('state', 'unknown')})")
    if len(channels) > 5:
        print(f"  ... and {len(channels) - 5} more")

    print(f"\n[QUEUES] Total: {len(queues)}")
    for queue in queues[:5]:
        msg_count = queue.get('messages', 0)
        print(f"  - {queue['name']} (messages: {msg_count})")
    if len(queues) > 5:
        print(f"  ... and {len(queues) - 5} more")

    print("\n" + "="*80 + "\n")


def monitor_loop():
    """Main monitoring loop"""
    logging.info("Starting RabbitMQ and Chaos Monkey monitor...")

    while True:
        try:
            chaos_healthy = check_chaos_monkey_health()
            chaos_stats = get_chaos_stats()
            stats = get_rabbitmq_stats()
            connections = get_connections()
            channels = get_channels()
            queues = get_queues()
            producer_health = get_dotnet_health(PRODUCER_URL, "PRODUCER")
            consumer_health = get_dotnet_health(CONSUMER_URL, "CONSUMER")

            print_report(stats, connections, channels, queues, chaos_healthy,
                         chaos_stats, producer_health, consumer_health)

        except Exception as e:
            logging.error(f"Monitor error: {e}")

        time.sleep(MONITOR_INTERVAL)


if __name__ == "__main__":
    monitor_loop()
