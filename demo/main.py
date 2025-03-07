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

# Dictionary to store the counts of requests in the past 1 second
request_counts1 = defaultdict(lambda: {'none': 0, 'proxied-by': 0, 'proxied-to': 0})
request_counts2 = defaultdict(lambda: {'none': 0, 'proxied-by': 0, 'proxied-to': 0})

# Dictionary to store the latest CPU usage for each pod
cpu_usage1 = defaultdict(float)
cpu_usage2 = defaultdict(float)

# Deque to store the timestamps of requests for each pod and proxied status
request_timestamps1 = defaultdict(lambda: {'none': deque(), 'proxied-by': deque(), 'proxied-to': deque()})
request_timestamps2 = defaultdict(lambda: {'none': deque(), 'proxied-by': deque(), 'proxied-to': deque()})

# Deque to store the proxy durations for each pod
proxy_durations1 = defaultdict(deque)
proxy_durations2 = defaultdict(deque)

# Lock for thread-safe updates to request_counts and request_timestamps
lock1 = threading.Lock()
lock2 = threading.Lock()

# Dictionary to map pod names to simplified names (numbers)
pod_name_map1 = {}
pod_name_map2 = {}

def get_simplified_pod_name(pod_name, pod_name_map):
    if pod_name not in pod_name_map:
        pod_name_map[pod_name] = len(pod_name_map) + 1
    return f"Pod {pod_name_map[pod_name]}"

def send_request(url, request_timestamps, proxy_durations, lock, pod_name_map):
    try:
        response = requests.get(url, timeout=3)
        pod_name = get_simplified_pod_name(response.headers.get('x-pod-name'), pod_name_map)
        is_proxied = response.headers.get('x-proxied-by') == response.headers.get('x-pod-name')
        proxy_duration = float(response.headers.get('x-proxy-duration', 0))
        with lock:
            current_time = time.time()
            if is_proxied:
                request_timestamps[pod_name]['proxied-by'].append(current_time)
                proxied_to_pod_name = get_simplified_pod_name(response.headers.get('x-proxied-to'), pod_name_map)
                request_timestamps[proxied_to_pod_name]['proxied-to'].append(current_time)
                proxy_durations[pod_name].append((current_time, proxy_duration))
            else:
                request_timestamps[pod_name]['none'].append(current_time)
    except requests.RequestException as e:
        print(f"Request failed: {e}")

def send_requests():
    while True:
        send_request(url1, request_timestamps1, proxy_durations1, lock1, pod_name_map1)
        send_request(url2, request_timestamps2, proxy_durations2, lock2, pod_name_map2)
        time.sleep(0.1)

def send_cpu_requests():
    while True:
        try:
            response1 = requests.get(cpu_url1, timeout=1)
            data1 = response1.json()
            with lock1:
                for pod in data1['podCpuUsage']:
                    simplified_pod_name = get_simplified_pod_name(pod, pod_name_map1)
                    cpu_usage1[simplified_pod_name] = data1['podCpuUsage'][pod]["cpuUsage"]
        except requests.RequestException as e:
            print(f"CPU request failed: {e}")

        try:
            response2 = requests.get(cpu_url2, timeout=1)
            data2 = response2.json()
            with lock2:
                for pod in data2['podCpuUsage']:
                    simplified_pod_name = get_simplified_pod_name(pod, pod_name_map2)
                    cpu_usage2[simplified_pod_name] = data2['podCpuUsage'][pod]["cpuUsage"]
        except requests.RequestException as e:
            print(f"CPU request failed: {e}")

        time.sleep(0.1)

def update_chart(frame):
    plt.clf()
    current_time = time.time()
    
    with lock1:
        # Update request_counts based on the timestamps in the past 1 second
        for pod_name in request_timestamps1:
            for proxied_status in ['none', 'proxied-by', 'proxied-to']:
                # Remove timestamps older than 5 seconds
                while request_timestamps1[pod_name][proxied_status] and current_time - request_timestamps1[pod_name][proxied_status][0] > 5:
                    request_timestamps1[pod_name][proxied_status].popleft()
                # Update the count of requests in the past 5 seconds
                request_counts1[pod_name][proxied_status] = len(request_timestamps1[pod_name][proxied_status])
    
    pod_names1 = list(request_counts1.keys())
    none_counts1 = [request_counts1[pod]['none'] for pod in pod_names1]
    proxiedby_counts1 = [request_counts1[pod]['proxied-by'] for pod in pod_names1]
    proxiedto_counts1 = [request_counts1[pod]['proxied-to'] for pod in pod_names1]

    with lock2:
        # Update request_counts based on the timestamps in the past 5 seconds
        for pod_name in request_timestamps2:
            for proxied_status in ['none', 'proxied-by', 'proxied-to']:
                # Remove timestamps older than 5 seconds
                while request_timestamps2[pod_name][proxied_status] and current_time - request_timestamps2[pod_name][proxied_status][0] > 5:
                    request_timestamps2[pod_name][proxied_status].popleft()
                # Update the count of requests in the past 5 seconds
                request_counts2[pod_name][proxied_status] = len(request_timestamps2[pod_name][proxied_status])
    
    pod_names2 = list(request_counts2.keys())
    none_counts2 = [request_counts2[pod]['none'] for pod in pod_names2]
    proxiedby_counts2 = [request_counts2[pod]['proxied-by'] for pod in pod_names2]
    proxiedto_counts2 = [request_counts2[pod]['proxied-to'] for pod in pod_names2]

    bar_width = 0.35
    index1 = range(len(pod_names1))
    index2 = range(len(pod_names2))
    
    plt.subplot(3, 4, 1)
    plt.bar(index1, none_counts1, bar_width, label='No proxying', color='b')
    plt.bar(index1, proxiedby_counts1, bar_width, bottom=none_counts1, label='Proxied by', color='r')
    plt.bar(index1, proxiedto_counts1, bar_width, bottom=proxiedby_counts1, label='Proxied to', color='g')

    plt.xlabel('Pod Name')
    plt.ylabel('Number of Requests')
    plt.title('Requests per Pod (Past 5s) - Cluster 1')
    plt.xticks(index1, pod_names1)
    plt.legend()
    plt.ylim(0, 20)

    plt.subplot(3, 4, 3)
    plt.bar(index2, none_counts2, bar_width, label='No proxying', color='b')
    plt.bar(index2, proxiedby_counts2, bar_width, bottom=none_counts2, label='Proxied by', color='r')
    plt.bar(index2, proxiedto_counts2, bar_width, bottom=proxiedby_counts2, label='Proxied to', color='g')

    plt.xlabel('Pod Name')
    plt.ylabel('Number of Requests')
    plt.title('Requests per Pod (Past 5s) - Cluster 2')
    plt.xticks(index2, pod_names2)
    plt.legend()
    plt.ylim(0, 20)

    plt.subplot(3, 4, 5)
    cpu_values1 = [cpu_usage1[pod] for pod in pod_names1]
    
    plt.bar(index1, cpu_values1, bar_width, color='g')
    
    plt.xlabel('Pod Name')
    plt.ylabel('CPU Usage')
    plt.title('CPU Usage per Pod - Cluster 1')
    plt.xticks(index1, pod_names1)
    plt.ylim(0, 100)

    # Add values on top of bars
    for i in range(len(cpu_values1)):
        plt.text(i, cpu_values1[i], str(round(cpu_values1[i])), ha='center', va='bottom')

    plt.subplot(3, 4, 7)
    cpu_values2 = [cpu_usage2[pod] for pod in pod_names2]
    
    plt.bar(index2, cpu_values2, bar_width, color='g')
    
    plt.xlabel('Pod Name')
    plt.ylabel('CPU Usage')
    plt.title('CPU Usage per Pod - Cluster 2')
    plt.xticks(index2, pod_names2)
    plt.ylim(0, 100)

    # Add values on top of bars
    for i in range(len(cpu_values2)):
        plt.text(i, cpu_values2[i], str(round(cpu_values2[i])), ha='center', va='bottom')

    plt.subplot(3, 4, 9)
    for pod_name in proxy_durations1:
        times, durations = zip(*proxy_durations1[pod_name])
        times = [t - current_time + 5 for t in times] # Adjust times to be relative to current time
        plt.plot(times, durations, label=pod_name)

    plt.xlabel('Time (s)')
    plt.ylabel('Proxy Duration (ms)')
    plt.title('Proxy Duration over Time - Cluster 1')
    plt.legend()

    plt.subplot(3, 4, 11)
    for pod_name in proxy_durations2:
        times, durations = zip(*proxy_durations2[pod_name])
        times = [t - current_time + 5 for t in times] # Adjust times to be relative to current time
        plt.plot(times, durations, label=pod_name)

    plt.xlabel('Time (s)')
    plt.ylabel('Proxy Duration (ms)')
    plt.title('Proxy Duration over Time - Cluster 2')
    plt.legend()

# Start the threads to send requests
threading.Thread(target=send_requests, daemon=True).start()
threading.Thread(target=send_cpu_requests, daemon=True).start()

# Set up the plot
fig = plt.figure()
ani = FuncAnimation(fig, update_chart, interval=30)

plt.show()