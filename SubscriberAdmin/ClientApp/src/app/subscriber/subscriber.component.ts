import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-subscriber',
  templateUrl: './subscriber.component.html',
  styleUrls: ['./subscriber.component.css']
})
export class SubscriberComponent implements OnInit {

  idToken: string = '';
  accessToken: string = '';

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router) { }

  ngOnInit(): void {
    this.route.queryParams
      .subscribe(params => {
        if (params['idToken']) {
          localStorage.setItem('idToken', params['idToken']);
        }

        this.idToken = localStorage.getItem('idToken') || '';

        if (params['accessToken']) {
          localStorage.setItem('accessToken', params['accessToken']);
          this.accessToken = localStorage.getItem('accessToken') || '';
        }

        this.accessToken = localStorage.getItem('accessToken') || '';

        if (!this.idToken) {
          this.router.navigate(['/']);
        } else {
          this.router.navigate(['/subscriber'], {});
        }
      });
  }

  sendMessage() {
    this.http.post('/api/line-notify-message?message=Hello', null).subscribe();

  }

  revoke() {
    this.http.post('/api/line-notify-revoke?idToken=' + this.idToken, null).subscribe(_ => {
      localStorage.removeItem('accessToken');
      window.location.reload();
    });
  }

}
