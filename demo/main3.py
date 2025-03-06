import requests
import time
import threading
import matplotlib.pyplot as plt
from collections import defaultdict, deque
from matplotlib.animation import FuncAnimation

# URLs to send requests to
url1 = "http://52.226.1.10/api/loadtest?n=1"
cpu_url1 = "http://52.226.1.10/.metrics/clusterHealth"
url2 = "http://128.203.73.89/api/loadtest?n=1"
cpu_url2 = "http://128.203.73.89/.metrics/clusterHealth"

# Dictionary to store the counts of requests in the past 1 second across all pods
request_counts = defaultdict(lambda: {'none': deque(), 'proxied-by': deque(), 'proxied-to': deque(), 'redirected-from': deque()})

# Dictionary to store the latest CPU usage for each pod
cpu_usage = defaultdict(deque)

# Dictionary to store the cluster CPU usage
cluster_cpu_usage1 = deque()
cluster_cpu_usage2 = deque()

# Deque to store the timestamps of requests for each pod and proxied status
request_timestamps = defaultdict(lambda: {'none': deque(), 'proxied-by': deque(), 'proxied-to': deque(), 'redirected-from': deque()})

# Deque to store the proxy durations for each pod
proxy_durations = defaultdict(deque)

# Lock for thread-safe updates to request_counts and request_timestamps
lock = threading.Lock()

# Dictionary to map pod names to simplified names (numbers)
pod_name_map = {}
pod_cluster_map = {}

def get_simplified_pod_name(pod_name, pod_name_map):
    if pod_name not in pod_name_map:
        pod_name_map[pod_name] = len(pod_name_map) + 1
    return f"Pod {pod_name_map[pod_name]}"

def send_request(url, request_timestamps, proxy_durations, lock, pod_name_map, cluster):
    try:
        response = requests.get(url, timeout=3, allow_redirects=True)
        history = response.history
        
        with lock:
            current_time = time.time()
            if history:
                for resp in history:
                    if resp.status_code == 302:
                        pod_name = get_simplified_pod_name(resp.headers.get('x-pod-name'), pod_name_map)
                        request_timestamps[pod_name]['redirected-from'].append(current_time)
                        pod_cluster_map[pod_name] = cluster

            final_pod_name = get_simplified_pod_name(response.headers.get('x-pod-name'), pod_name_map)
            is_proxied = response.headers.get('x-proxied-by') == response.headers.get('x-pod-name')
            proxy_duration = float(response.headers.get('x-proxy-duration', 0))
            
            if is_proxied:
                request_timestamps[final_pod_name]['proxied-by'].append(current_time)
                proxied_to_pod_name = get_simplified_pod_name(response.headers.get('x-proxied-to'), pod_name_map)
                request_timestamps[proxied_to_pod_name]['proxied-to'].append(current_time)
                proxy_durations[final_pod_name].append((current_time, proxy_duration))
            else:
                request_timestamps[final_pod_name]['none'].append(current_time)
            
            pod_cluster_map[final_pod_name] = cluster

    except requests.RequestException as e:
        print(f"Request failed: {e}")

def send_requests():
    while True:
        send_request(url1, request_timestamps, proxy_durations, lock, pod_name_map, 'Cluster 1')
        #send_request(url2, request_timestamps, proxy_durations, lock, pod_name_map, 'Cluster 2')
        time.sleep(0.1)

def send_cpu_requests():
    while True:
        try:
            response1 = requests.get(cpu_url1, timeout=1)
            data1 = response1.json()
            with lock:
                for pod in data1['podCpuUsage']:
                    simplified_pod_name = get_simplified_pod_name(pod, pod_name_map)
                    cpu_usage[simplified_pod_name].append(data1['podCpuUsage'][pod]["cpuUsage"])
                    pod_cluster_map[simplified_pod_name] = 'Cluster 1'
                cluster_cpu_usage1.append(data1["clusterCpuUsage"])
        except requests.RequestException as e:
            print(f"CPU request failed: {e}")

        try:
            response2 = requests.get(cpu_url2, timeout=1)
            data2 = response2.json()
            with lock:
                for pod in data2['podCpuUsage']:
                    simplified_pod_name = get_simplified_pod_name(pod, pod_name_map)
                    cpu_usage[simplified_pod_name].append(data2['podCpuUsage'][pod]["cpuUsage"])
                    pod_cluster_map[simplified_pod_name] = 'Cluster 2'
                cluster_cpu_usage2.append(data2["clusterCpuUsage"])
        except requests.RequestException as e:
            print(f"CPU request failed: {e}")

        time.sleep(0.9)

def update_chart(frame):
    current_time = time.time()
    
    with lock:
        # Update request_counts based on the timestamps in the past 5 seconds
        for pod_name in request_timestamps:
            for proxied_status in ['none', 'proxied-by', 'proxied-to', 'redirected-from']:
                # Remove timestamps older than 5 seconds
                while request_timestamps[pod_name][proxied_status] and current_time - request_timestamps[pod_name][proxied_status][0] > 5:
                    request_timestamps[pod_name][proxied_status].popleft()
                # Update the count of requests in the past 5 seconds
                request_counts[pod_name][proxied_status].append(len(request_timestamps[pod_name][proxied_status]))
    
    fig.suptitle('Cluster Metrics', fontsize=16)

    # Cluster 1 RPS timeseries
    axs[0, 0].clear()
    for proxied_status in ['proxied-by', 'redirected-from']:
        for pod in [p for p in request_counts if pod_cluster_map.get(p) == 'Cluster 1']:
            axs[0, 0].plot(list(request_counts[pod][proxied_status]), label=f'{pod} - {proxied_status}')
    axs[0, 0].set_xlabel('Time')
    axs[0, 0].set_ylabel('RPS')
    axs[0, 0].set_title('RPS per Pod (Cluster 1)')
    axs[0, 0].legend()

    # Cluster 2 RPS timeseries
    axs[0, 1].clear()
    for proxied_status in ['none']:
        for pod in [p for p in request_counts if pod_cluster_map.get(p) == 'Cluster 2']:
            axs[0, 1].plot(list(request_counts[pod][proxied_status]), label=f'{pod} - {proxied_status}')
    axs[0, 1].set_xlabel('Time')
    axs[0, 1].set_ylabel('RPS')
    axs[0, 1].set_title('RPS per Pod (Cluster 2)')
    axs[0, 1].legend()

    # Cluster 1 CPU Utilization per Pod timeseries
    axs[1, 0].clear()
    for pod in [p for p in cpu_usage if pod_cluster_map.get(p) == 'Cluster 1']:
        axs[1, 0].plot(list(cpu_usage[pod]), label=pod)
    axs[1, 0].set_xlabel('Time')
    axs[1, 0].set_ylabel('CPU Usage')
    axs[1, 0].set_title('CPU Usage per Pod (Cluster 1)')
    axs[1, 0].legend()

    # Cluster 2 CPU Utilization per Pod timeseries
    axs[1, 1].clear()
    for pod in [p for p in cpu_usage if pod_cluster_map.get(p) == 'Cluster 2']:
        axs[1, 1].plot(list(cpu_usage[pod]), label=pod)
    axs[1, 1].set_xlabel('Time')
    axs[1, 1].set_ylabel('CPU Usage')
    axs[1, 1].set_title('CPU Usage per Pod (Cluster 2)')
    axs[1, 1].legend()

    # Cluster 1 CPU Utilization of the Cluster timeseries
    axs[2, 0].clear()
    axs[2, 0].plot(list(cluster_cpu_usage1), color='b')
    axs[2, 0].set_xlabel('Time')
    axs[2, 0].set_ylabel('Cluster CPU Usage')
    axs[2, 0].set_title('Cluster CPU Usage (Cluster 1)')

    # Cluster 2 CPU Utilization of the Cluster timeseries
    axs[2, 1].clear()
    axs[2, 1].plot(list(cluster_cpu_usage2), color='b')
    axs[2, 1].set_xlabel('Time')
    axs[2, 1].set_ylabel('Cluster CPU Usage')
    axs[2, 1].set_title('Cluster CPU Usage (Cluster 2)')

    # Cluster 1 Proxy Duration per Pod timeseries
    axs[3, 0].clear()
    for pod_name in [p for p in proxy_durations if pod_cluster_map.get(p) == 'Cluster 1']:
        times, durations = zip(*proxy_durations[pod_name])
        times = [t - current_time + 5 for t in times] # Adjust times to be relative to current time
        axs[3, 0].plot(times, durations, label=pod_name)
    axs[3, 0].set_xlabel('Time (s)')
    axs[3, 0].set_ylabel('Proxy Duration (ms)')
    axs[3, 0].set_title('Proxy Duration per Pod (Cluster 1)')
    axs[3, 0].legend()

    # Cluster 2 Proxy Duration per Pod timeseries
    axs[3, 1].clear()
    for pod_name in [p for p in proxy_durations if pod_cluster_map.get(p) == 'Cluster 2']:
        times, durations = zip(*proxy_durations[pod_name])
        times = [t - current_time + 5 for t in times] # Adjust times to be relative to current time
        axs[3, 1].plot(times, durations, label=pod_name)
    axs[3, 1].set_xlabel('Time (s)')
    axs[3, 1].set_ylabel('Proxy Duration (ms)')
    axs[3, 1].set_title('Proxy Duration per Pod (Cluster 2)')
    axs[3, 1].legend()

    plt.tight_layout()
    plt.draw()

# Set up the plot
fig, axs = plt.subplots(4, 2, figsize=(15, 10))
ani = FuncAnimation(fig, update_chart, interval=1000)

# Start the threads to send requests
threading.Thread(target=send_requests, daemon=True).start()
threading.Thread(target=send_cpu_requests, daemon=True).start()

plt.show()
